import React, { useEffect, useMemo, useRef, useState } from 'react'
import {
  AccessCredentialDto,
  AdminAuditLogDto,
  AdminAppReleaseDto,
  AdminFaqItem,
  AdminNotificationDeliveryDto,
  AdminDashboardSummaryDto,
  AdminReferralProgramDto,
  AdminRewardLedgerDto,
  AdminSessionDto,
  AdminTelegramBotConnectionCheckDto,
  AdminTelegramBotSettingsDto,
  AdminTariffDto,
  AdminUserDto,
  AdminUserOverviewDto,
  ApiClient,
  ApiClientError,
  AppReleaseOverviewDto,
  AppReleaseUpsertPayload,
  CreateServerPayload,
  CreateVpnInboundPayload,
  CreateVpnPanelPayload,
  FaqOverviewDto,
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
  SiteContentBlockUpsertPayload,
  SiteContentBlockDto,
  SiteContentReadinessDto,
  SubscriptionDto,
  SupportConversationDto,
  SupportMessageDto,
  UpdateTariffPayload,
  UpdateAdminUserPayload,
  UpdateTelegramBotSettingsPayload,
  UpsertPaymentProviderAccountPayload,
  VpnClientDto,
  VpnInboundDto,
  VpnNodeDto,
  VpnPanelDto,
  WorkScenarioDto,
  WorkScenarioUpsertPayload,
  PanelHealthCheckDto,
  PanelSyncRunDto,
  normalizeApiError
} from '@vpn-platform/api-client'
import { Card, CodeBlock, ConfirmButton, CopyButton, EmptyState, ErrorBlock, FormValidationSummary, formatReferralRewardType, formatReferralRewardValue, formatStatusLabel, LoadingBlock, PageShell, PasswordField, PrimaryButton, QrCodePreview, SecretField, SectionCard, SkipLink, StatTile, StatusBadge, ValidationModeBadge } from '@vpn-platform/ui'
import { adminUserToEditForm, buildAdminUserOverviewStats, formatAdminMoney, isAdminUserEditFormChanged, telegramDisplayName, validateAdminUserEditForm, type AdminUserEditForm } from './admin-users'
import { formatAdminDisplayLabel, formatAdminRoleLabels } from './admin-display-labels'
import { adminSectionIds, adminSectionLabels, canAccessAdminSection, canWriteAdminSection, parseAdminSectionHref, type AdminSectionId } from './admin-capabilities'
import { getAdminPageMetadata } from './admin-page-metadata'
import { isTelegramBotSettingsFormChanged, telegramBotSettingsToForm } from './admin-telegram-settings'
import { isAppReleaseFormChanged, isFaqFormChanged, isSiteContentFormChanged, isTariffFormChanged, isWorkScenarioFormChanged, normalizeAppReleasePayload, normalizeFaqPayload, normalizeSiteContentPayload, normalizeTariffPayload, normalizeWorkScenarioPayload, parseWorkScenarioTariffIds, scenarioTariffIdsToJson, tariffFeaturesTextToJson } from './admin-managed-editors'
import { getAdminAccessCommandBlocker, getAdminAccessTerminalReason, getNextAdminAccessExpiryDelay, isAdminAccessExpired } from './admin-accesses'
import { getAdminSubscriptionActionAvailability, getAdminSubscriptionActionBlocker, getAdminSubscriptionEffectiveEndTime, getNextAdminSubscriptionExpiryDelay, type AdminSubscriptionAction } from './admin-subscriptions'
import { canCancelProvisioningRun, canRetryProvisioningRun, isProvisioningStateConflict } from './provisioning-state'
import { adminAccessDeniedMessage, adminSessionEndedMessage, isAdminAccessTokenExpired, isAdminSessionRejected } from './admin-session'
import { isOptionalSafeAdminHttpUrl, validateTelegramBotUrlFields } from './admin-url-validation'
import { validateServerForm } from './admin-server-validation'
import { isVpnInboundFormChanged, isVpnPanelFormChanged } from './admin-vpn-editors'
import { getAdminOrderPaymentRecheckBlocker, getAdminPaymentRecheckBlocker } from './admin-payments'
import { buildReferralProgramPayload, defaultReferralProgramForm, isReferralProgramFormChanged, referralProgramToForm, validateReferralProgramForm, type ReferralProgramFormState } from './admin-referrals'
import { validateWorkScenarioForm } from './admin-work-scenarios'

const api = new ApiClient(import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080')
const TOKEN_STORAGE_KEY = 'vpn-platform-admin-token'
const REFRESH_TOKEN_STORAGE_KEY = 'vpn-platform-admin-refresh-token'
const ADMIN_EMAIL_STORAGE_KEY = 'vpn-platform-admin-email'
const yookassaAllowedIps = '185.71.76.0/27,185.71.77.0/27,77.75.153.0/25,77.75.156.11,77.75.156.35,77.75.154.128/25,2a02:5180::/32'
const paymentProviderOptions: PaymentProvider[] = ['YooKassa', 'RoboKassa', 'YooMoney', 'TelegramStars', 'CloudPayments', 'TBankAcquiring', 'Prodamus', 'Stripe', 'PayPal']
const adminAuthRequiredMessage = 'Войдите как администратор, чтобы включить загрузку данных и действия в разделах.'

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


const adminSections = adminSectionIds.map((id) => [id, adminSectionLabels[id]] as const)

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

const adminSectionLoadAreas: Record<AdminSectionId, readonly string[]> = {
  dashboard: ['dashboard', 'orders', 'payments', 'обращения поддержки', 'подготовка серверов'],
  users: ['users'],
  support: ['обращения поддержки'],
  audit: ['аудит', 'email-уведомления'],
  payments: ['orders', 'payments', 'способы оплаты', 'события оплат', 'refunds'],
  tariffs: ['tariffs', 'сценарии работы'],
  referrals: ['реферальные программы', 'реферальные начисления'],
  subscriptions: ['subscriptions', 'servers'],
  vpn: ['accesses'],
  nodes: ['servers'],
  panels: ['VPN-панели'],
  provisioning: ['подготовка серверов', 'servers'],
  bot: ['настройки Telegram-бота'],
  releases: ['Что нового', 'сводка релизов'],
  faq: ['FAQ', 'сводка FAQ'],
  content: ['контент сайта', 'готовность главной'],
  scenarios: ['сценарии работы', 'tariffs']
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
  if (normalized === 'dry-run') return 'Проверка без изменений'
  if (normalized === 'validation-deploy') return 'Проверочное развёртывание'
  if (normalized === 'live-deploy') return 'Рабочее развёртывание'
  if (normalized === 'live-deploy-blocked') return 'Рабочее развёртывание заблокировано'
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

  return parseAdminSectionHref(window.location.hash) ?? adminSections[0][0]
}

function supportActionResourceKey(conversationId: string) {
  return `support:${conversationId}`
}

function userActionResourceKey(userId: string) {
  return `user:${userId}`
}

function subscriptionActionResourceKey(subscriptionId: string) {
  return `subscription:${subscriptionId}`
}

function accessActionResourceKey(accessId: string) {
  return `access:${accessId}`
}

function subscriptionActionResourceKeys(subscriptionId: string, accessId: string | null | undefined) {
  return accessId
    ? [subscriptionActionResourceKey(subscriptionId), accessActionResourceKey(accessId)]
    : [subscriptionActionResourceKey(subscriptionId)]
}

function vpnPanelActionResourceKey(panelId: string) {
  return `vpn-panel:${panelId}`
}

function vpnInboundActionResourceKey(inboundId: string) {
  return `vpn-inbound:${inboundId}`
}

function vpnClientActionResourceKey(clientId: string) {
  return `vpn-client:${clientId}`
}

function serverActionResourceKey(serverId: string) {
  return `server:${serverId}`
}

function provisioningRunActionResourceKey(runId: string) {
  return `provisioning-run:${runId}`
}

function paymentProviderActionResourceKey(accountId: string) {
  return `payment-provider:${accountId}`
}

const referralProgramActionResourceKey = 'referral-program:form'

function notificationDeliveryActionResourceKey(deliveryId: string) {
  return `notification-delivery:${deliveryId}`
}

function orderActionResourceKey(orderId: string) {
  return `order:${orderId}`
}

function paymentActionResourceKey(paymentId: string) {
  return `payment:${paymentId}`
}

function paymentActionResourceKeys(paymentId: string, orderId?: string | null) {
  return orderId
    ? [paymentActionResourceKey(paymentId), orderActionResourceKey(orderId)]
    : [paymentActionResourceKey(paymentId)]
}

function refundActionResourceKeys(refund: RefundDto) {
  return [`refund:${refund.id}`, paymentActionResourceKey(refund.paymentAttemptId)]
}

function tariffActionResourceKey(tariffId: string) {
  return `tariff:${tariffId}`
}

function appReleaseActionResourceKey(releaseId: string) {
  return `app-release:${releaseId}`
}

function faqActionResourceKey(faqId: string) {
  return `faq:${faqId}`
}

const siteContentActionResourceKey = 'site-content'

function workScenarioActionResourceKey(scenarioId: string) {
  return `work-scenario:${scenarioId}`
}

const botSettingsActionResourceKey = 'bot-settings'

type GenericUser = AdminUserDto
type ServerFormState = CreateServerPayload
type LoadError = { area: string; message: string }
type AdminLoadRequest = {
  operationId: number
  token: string
  session: AdminSessionDto
  promise: Promise<boolean>
}
type AdminUsersLoadRequest = {
  operationId: number
  token: string
  search: string
  status: string
  promise: Promise<boolean>
}
type AdminDetailLoadRequest = {
  operationId: number
  token: string
  entityId: string
  promise: Promise<boolean>
}
type AdminSessionCommandRequest = {
  operationId: number
  key: string
  promise: Promise<void>
}

const defaultAdminUserEditForm: AdminUserEditForm = {
  displayName: '',
  status: 'New',
  isBlocked: false
}

type AdminActionContext = {
  operationId: number
  isCurrent: () => boolean
  reloadAll: () => Promise<boolean>
}
type VpnConflictDraft =
  | { kind: 'panel'; id: string; panelId: string; form: CreateVpnPanelPayload }
  | { kind: 'inbound'; id: string; panelId: string; form: CreateVpnInboundPayload }
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
  revision: inbound.revision,
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
  visibleFrom: null,
  visibleTo: null,
  tariffType: 'Personal',
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

const appReleaseIdPattern = /^[a-z0-9]+(?:-[a-z0-9]+)*$/
const appReleaseItemTypePattern = /^(new|improved|fixed|important)$/

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
  revision: 0,
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
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString('ru-RU', { dateStyle: 'short', timeStyle: 'medium' })
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
  if (getRefundableAmount(payment) <= 0) return 'Сумма уже возвращена.'
  if (payment.status !== 'Succeeded' && payment.status !== 'PartiallyRefunded') return 'Возврат доступен только после успешной оплаты.'
  return ''
}

function refundRecheckBlockerText(refund: RefundDto) {
  if (refund.recheckBlockers && refund.recheckBlockers.length > 0) return refund.recheckBlockers.join(' · ')
  if (refund.recheckSupported === false) return 'Провайдер не поддерживает сверку отдельного возврата.'
  if (refund.canRecheck !== true) return 'Возврат уже завершён или не имеет идентификатора провайдера.'
  return ''
}

function refundRetryBlockerText(refund: RefundDto) {
  if (refund.retryBlockers && refund.retryBlockers.length > 0) return refund.retryBlockers.join(' · ')
  if (refund.retrySupported === false) return 'Провайдер не гарантирует безопасный повтор создания возврата.'
  if (refund.canRetry !== true) return 'Возврат нельзя безопасно повторить.'
  return ''
}

function homeContentIssueCount(readiness: SiteContentReadinessDto | null) {
  if (!readiness) return 0
  return readiness.missingKeys.length + readiness.inactiveKeys.length + readiness.emptyKeys.length + readiness.duplicateKeys.length
}

function parseTariffFeatures(tariff: Pick<AdminTariffDto, 'features' | 'featuresJson'> | UpdateTariffPayload) {
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

function featuresToText(tariff: Pick<AdminTariffDto, 'features' | 'featuresJson'> | UpdateTariffPayload) {
  return parseTariffFeatures(tariff).join('\n')
}

function validateTariffForm(form: UpdateTariffPayload, featuresText = '') {
  const errors: string[] = []
  const currency = String(form.currency ?? '').trim().toUpperCase()
  const slug = String(form.slug ?? '').trim()
  const fieldLengths: Array<[string, unknown, number]> = [
    ['Название', form.name, 200],
    ['Slug', form.slug, 160],
    ['Короткое описание', form.description, 500],
    ['Полное описание', form.fullDescription, 4000],
    ['Бейдж', form.badge, 80],
    ['Категория', form.category, 120],
    ['Разрешенные регионы', form.allowedRegionsCsv, 2000],
    ['Группы серверов', form.allowedNodeGroupsCsv, 2000],
    ['Сценарий выдачи', form.provisioningScenario, 120],
    ['Текст после оплаты', form.afterPaymentText, 2000]
  ]

  if (!String(form.name ?? '').trim()) errors.push('Укажите название тарифа.')
  if (n(form.price) < 0) errors.push('Цена не может быть отрицательной.')
  if (n(form.durationDays) <= 0) errors.push('Срок тарифа должен быть больше 0 дней.')
  if (n(form.maxDevices) <= 0) errors.push('Количество устройств должно быть больше 0.')
  if (!/^[A-Z]{3}$/.test(currency)) errors.push('Валюта должна быть кодом из 3 латинских букв: RUB, USD или XTR.')
  if (slug && !/^[a-z0-9а-яё_-]+(?:-[a-z0-9а-яё_-]+)*$/i.test(slug)) errors.push('Slug может содержать буквы, цифры, дефис и подчёркивание.')
  for (const [label, value, limit] of fieldLengths) {
    if (String(value ?? '').trim().length > limit) errors.push(`${label}: не более ${limit} символов.`)
  }
  if (tariffFeaturesTextToJson(featuresText).length > 4000) errors.push('Преимущества: общий объем не должен превышать 4000 символов.')
  if (form.visibleFrom && form.visibleTo && new Date(form.visibleFrom) > new Date(form.visibleTo)) errors.push('Начало публикации должно быть раньше окончания.')

  return errors
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
  if (!isOptionalSafeAdminHttpUrl(form.apiBaseUrl)) errors.push('API base URL должен быть корректным http/https адресом без логина и пароля.')
  if (!isOptionalSafeAdminHttpUrl(form.returnUrl)) errors.push('Return URL должен быть корректным http/https адресом без логина и пароля.')
  if (!isOptionalSafeAdminHttpUrl(form.webhookUrl)) errors.push('Webhook URL должен быть корректным http/https адресом без логина и пароля.')
  for (const field of setup.extraSettingsFields.filter((item) => item.inputMode === 'url')) {
    if (!isOptionalSafeAdminHttpUrl(providerExtraSettingValue(form, field))) errors.push(`${field.label} должен быть корректным http/https адресом без логина и пароля.`)
  }
  return errors
}

function parseJsonObject(value: string): Record<string, unknown> | null {
  try {
    const parsed = JSON.parse(value)
    return parsed !== null && typeof parsed === 'object' && !Array.isArray(parsed)
      ? parsed as Record<string, unknown>
      : null
  } catch {
    return null
  }
}

function validateVpnPanelForm(form: CreateVpnPanelPayload, isEditing: boolean) {
  const errors: string[] = []
  const capacity = Number(form.capacity)
  if (!form.name.trim()) errors.push('Укажите название 3x-ui панели.')
  if (form.name.trim().length > 200) errors.push('Название 3x-ui панели не должно превышать 200 символов.')
  if (form.baseUrl.trim().length > 2048) errors.push('Адрес 3x-ui панели не должен превышать 2048 символов.')
  if (!form.baseUrl.trim()) {
    errors.push('Укажите адрес 3x-ui панели.')
  } else if (!isOptionalSafeAdminHttpUrl(form.baseUrl)) {
    errors.push('Адрес 3x-ui панели должен быть корректным http/https URL без логина и пароля.')
  }
  if (!form.login.trim()) errors.push('Укажите логин 3x-ui панели.')
  if (form.login.trim().length > 200) errors.push('Логин 3x-ui панели не должен превышать 200 символов.')
  if ((form.password?.length ?? 0) > 4096) errors.push('Пароль 3x-ui панели не должен превышать 4096 символов.')
  if (form.region.trim().length > 120) errors.push('Регион 3x-ui панели не должен превышать 120 символов.')
  if (!isEditing && !form.password?.trim()) errors.push('Укажите пароль 3x-ui панели.')
  if (!Number.isInteger(capacity) || capacity <= 0) errors.push('Емкость 3x-ui панели должна быть целым числом больше 0.')
  if (form.defaultInboundTemplateJson.length > 32768) errors.push('Шаблон inbound не должен превышать 32768 символов.')
  else if (!parseJsonObject(form.defaultInboundTemplateJson)) errors.push('Шаблон inbound должен быть корректным JSON-объектом.')
  return errors
}

function validateInboundForm(form: CreateVpnInboundPayload, selectedVpnPanelId: string) {
  const errors: string[] = []
  const port = Number(form.port)
  const capacity = Number(form.capacity)
  if (!selectedVpnPanelId) errors.push('Выберите 3x-ui панель перед созданием inbound.')
  if (!form.name.trim()) errors.push('Укажите название inbound-правила.')
  if (form.name.trim().length > 200) errors.push('Название inbound-правила не должно превышать 200 символов.')
  if (form.listen.trim().length > 255) errors.push('Listen inbound не должен превышать 255 символов.')
  if (!Number.isInteger(port) || port <= 0 || port > 65535) errors.push('Порт inbound должен быть целым числом в диапазоне 1-65535.')
  if (!['vless', 'vmess', 'trojan'].includes(form.protocol.trim().toLowerCase())) errors.push('Протокол inbound должен быть VLESS, VMess или Trojan.')
  if (!Number.isInteger(capacity) || capacity <= 0) errors.push('Емкость inbound должна быть целым числом больше 0.')
  if (form.isDefault && !form.isActive) errors.push('Основной inbound должен быть активен.')
  if (form.settingsJson.length > 32768) errors.push('settingsJson не должен превышать 32768 символов.')
  else if (!parseJsonObject(form.settingsJson)) errors.push('settingsJson должен быть корректным JSON-объектом.')
  const streamSettings = form.streamSettingsJson.length <= 32768 ? parseJsonObject(form.streamSettingsJson) : null
  if (form.streamSettingsJson.length > 32768) errors.push('streamSettingsJson не должен превышать 32768 символов.')
  if (!streamSettings) {
    errors.push('streamSettingsJson должен быть корректным JSON-объектом.')
  } else if (typeof streamSettings.network !== 'string' || !streamSettings.network.trim()) {
    errors.push('streamSettingsJson должен содержать непустое поле network.')
  }
  if (form.sniffingJson.length > 32768) errors.push('sniffingJson не должен превышать 32768 символов.')
  else if (!parseJsonObject(form.sniffingJson)) errors.push('sniffingJson должен быть корректным JSON-объектом.')
  return errors
}

function toDateTimeLocalValue(value?: string | null) {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const offset = date.getTimezoneOffset()
  const local = new Date(date.getTime() - offset * 60_000)
  return local.toISOString().slice(0, 16)
}

function fromDateTimeLocalValue(value: string) {
  return value ? new Date(value).toISOString() : new Date().toISOString()
}

function fromOptionalDateTimeLocalValue(value: string) {
  return value ? fromDateTimeLocalValue(value) : null
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
  if (account.mode === 'Disabled') return 'Режим провайдера выключен.'
  if (!account.shopId) return 'Нужен ShopId или merchant identifier.'
  if (account.provider !== 'TelegramStars' && account.mode === 'Production' && !account.hasSecretKey) return 'Для рабочего режима нужен защищенный secret key.'
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
  const [adminDataReady, setAdminDataReady] = useState(false)
  const adminAccessVerified = Boolean(adminSession)
  const adminDisabledTitle = adminAccessVerified ? undefined : adminAuthRequiredMessage
  const [email, setEmail] = useState(readSessionStorageItem(ADMIN_EMAIL_STORAGE_KEY) ?? '')
  const [password, setPassword] = useState('')
  const [rememberAdminEmail, setRememberAdminEmail] = useState(() => Boolean(readSessionStorageItem(ADMIN_EMAIL_STORAGE_KEY)))
  const [users, setUsers] = useState<GenericUser[]>([])
  const [userSearch, setUserSearch] = useState('')
  const [userStatusFilter, setUserStatusFilter] = useState('')
  const [usersLoading, setUsersLoading] = useState(false)
  const [usersError, setUsersError] = useState('')
  const [selectedUserId, setSelectedUserId] = useState('')
  const [userOverview, setUserOverview] = useState<AdminUserOverviewDto | null>(null)
  const [userOverviewLoading, setUserOverviewLoading] = useState(false)
  const [userOverviewError, setUserOverviewError] = useState('')
  const [userEditForm, setUserEditForm] = useState<AdminUserEditForm>(defaultAdminUserEditForm)
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
  const [adminAccessClockTick, setAdminAccessClockTick] = useState(0)
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
  const [supportMessagesLoading, setSupportMessagesLoading] = useState(false)
  const [supportMessagesError, setSupportMessagesError] = useState('')
  const [supportReplyText, setSupportReplyText] = useState('')
  const [supportNoteText, setSupportNoteText] = useState('')
  const [tariffs, setTariffs] = useState<AdminTariffDto[]>([])
  const [tariffForm, setTariffForm] = useState<UpdateTariffPayload>(defaultTariffForm)
  const [tariffFeaturesText, setTariffFeaturesText] = useState('')
  const [editingTariffId, setEditingTariffId] = useState('')
  const [editingTariffRevision, setEditingTariffRevision] = useState<number | null>(null)
  const [referralPrograms, setReferralPrograms] = useState<AdminReferralProgramDto[]>([])
  const [referralRewards, setReferralRewards] = useState<AdminRewardLedgerDto[]>([])
  const [referralProgramForm, setReferralProgramForm] = useState<ReferralProgramFormState>(defaultReferralProgramForm)
  const [editingReferralProgramId, setEditingReferralProgramId] = useState('')
  const [editingProviderAccountId, setEditingProviderAccountId] = useState('')
  const [appReleases, setAppReleases] = useState<AdminAppReleaseDto[]>([])
  const [appReleaseOverview, setAppReleaseOverview] = useState<AppReleaseOverviewDto | null>(null)
  const [releaseVisibilityFilter, setReleaseVisibilityFilter] = useState('all')
  const [releaseSourceFilter, setReleaseSourceFilter] = useState('all')
  const [releaseSearch, setReleaseSearch] = useState('')
  const [releaseForm, setReleaseForm] = useState<AppReleaseUpsertPayload>(defaultReleaseForm)
  const [editingReleaseId, setEditingReleaseId] = useState('')
  const [faqEntries, setFaqEntries] = useState<AdminFaqItem[]>([])
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
  const vpnPanelDetailsRequestId = useRef(0)
  const vpnPanelDetailsLoadInFlight = useRef<AdminDetailLoadRequest | null>(null)
  const [vpnPanelDetailsLoading, setVpnPanelDetailsLoading] = useState(false)
  const [vpnPanelDetailsError, setVpnPanelDetailsError] = useState('')
  const [vpnInbounds, setVpnInbounds] = useState<VpnInboundDto[]>([])
  const [vpnMigrationInbounds, setVpnMigrationInbounds] = useState<VpnInboundDto[]>([])
  const [vpnClients, setVpnClients] = useState<VpnClientDto[]>([])
  const [vpnClientMigrationTargets, setVpnClientMigrationTargets] = useState<Record<string, string>>({})
  const [subscriptionMigrationTargets, setSubscriptionMigrationTargets] = useState<Record<string, string>>({})
  const [vpnHealthChecks, setVpnHealthChecks] = useState<PanelHealthCheckDto[]>([])
  const [vpnSyncRuns, setVpnSyncRuns] = useState<PanelSyncRunDto[]>([])
  const [botSettings, setBotSettings] = useState<AdminTelegramBotSettingsDto>(defaultBotSettings)
  const [botSettingsForm, setBotSettingsForm] = useState<UpdateTelegramBotSettingsPayload>({})
  const [botSettingsCheck, setBotSettingsCheck] = useState<AdminTelegramBotConnectionCheckDto | null>(null)
  const botSettingsCheckRequestId = useRef(0)
  const [loadErrors, setLoadErrors] = useState<LoadError[]>([])
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [busy, setBusy] = useState(false)
  const [sessionHydrating, setSessionHydrating] = useState(Boolean(token))
  const [logoutBusy, setLogoutBusy] = useState(false)
  const [actionBusyResourceKeys, setActionBusyResourceKeys] = useState<ReadonlySet<string>>(() => new Set())
  const isActionResourceBusy = (...resourceKeys: string[]) => resourceKeys.some((key) => actionBusyResourceKeys.has(key))
  const [serverForm, setServerForm] = useState<ServerFormState>(defaultServerForm)
  const [editingServerId, setEditingServerId] = useState<string | null>(null)
  const [editingServerRevision, setEditingServerRevision] = useState<number | null>(null)
  const [providerForm, setProviderForm] = useState<UpsertPaymentProviderAccountPayload>(defaultProviderForm)
  const [vpnPanelForm, setVpnPanelForm] = useState<CreateVpnPanelPayload>(defaultVpnPanelForm)
  const [editingVpnPanelId, setEditingVpnPanelId] = useState<string | null>(null)
  const [inboundForm, setInboundForm] = useState<CreateVpnInboundPayload>(defaultInboundForm)
  const [editingInboundId, setEditingInboundId] = useState<string | null>(null)
  const [subscriptionExtendDays, setSubscriptionExtendDays] = useState<Record<string, number>>({})
  const [activeSection, setActiveSection] = useState<AdminSectionId>(() => readAdminSectionFromHash())
  const restoredSessionHydrationStarted = useRef(false)
  const sessionOperationId = useRef(0)
  const loadAllRequestId = useRef(0)
  const loadAllInFlight = useRef<AdminLoadRequest | null>(null)
  const loginRequestInFlight = useRef<AdminSessionCommandRequest | null>(null)
  const refreshSessionRequestInFlight = useRef<AdminSessionCommandRequest | null>(null)
  const logoutRequestInFlight = useRef<AdminSessionCommandRequest | null>(null)
  const usersRequestId = useRef(0)
  const usersLoadInFlight = useRef<AdminUsersLoadRequest | null>(null)
  const actionRequestsInFlight = useRef(new Set<string>())
  const actionResourceOwners = useRef(new Map<string, string>())
  const userOverviewRequestId = useRef(0)
  const userOverviewLoadInFlight = useRef<AdminDetailLoadRequest | null>(null)
  const supportMessagesRequestId = useRef(0)
  const supportMessagesLoadInFlight = useRef<AdminDetailLoadRequest | null>(null)
  const selectedUserIdRef = useRef(selectedUserId)
  const selectedSupportConversationIdRef = useRef(selectedSupportConversationId)
  const selectedVpnPanelIdRef = useRef(selectedVpnPanelId)
  const providerFormRef = useRef(providerForm)
  const editingProviderAccountIdRef = useRef(editingProviderAccountId)
  const tariffFormRef = useRef(tariffForm)
  const tariffFeaturesTextRef = useRef(tariffFeaturesText)
  const editingTariffIdRef = useRef(editingTariffId)
  const referralProgramFormRef = useRef(referralProgramForm)
  const editingReferralProgramIdRef = useRef(editingReferralProgramId)
  const releaseFormRef = useRef(releaseForm)
  const editingReleaseIdRef = useRef(editingReleaseId)
  const faqFormRef = useRef(faqForm)
  const editingFaqIdRef = useRef(editingFaqId)
  const siteContentFormRef = useRef(siteContentForm)
  const editingSiteContentIdRef = useRef(editingSiteContentId)
  const workScenarioFormRef = useRef(workScenarioForm)
  const editingWorkScenarioIdRef = useRef(editingWorkScenarioId)
  const vpnPanelFormRef = useRef(vpnPanelForm)
  const editingVpnPanelIdRef = useRef(editingVpnPanelId)
  const inboundFormRef = useRef(inboundForm)
  const editingInboundIdRef = useRef(editingInboundId)
  const serverFormRef = useRef(serverForm)
  const editingServerIdRef = useRef(editingServerId)
  const botSettingsFormRef = useRef(botSettingsForm)
  const botSettingsFormDirty = useRef(false)
  selectedUserIdRef.current = selectedUserId
  selectedSupportConversationIdRef.current = selectedSupportConversationId
  selectedVpnPanelIdRef.current = selectedVpnPanelId
  providerFormRef.current = providerForm
  editingProviderAccountIdRef.current = editingProviderAccountId
  tariffFormRef.current = tariffForm
  tariffFeaturesTextRef.current = tariffFeaturesText
  editingTariffIdRef.current = editingTariffId
  referralProgramFormRef.current = referralProgramForm
  editingReferralProgramIdRef.current = editingReferralProgramId
  releaseFormRef.current = releaseForm
  editingReleaseIdRef.current = editingReleaseId
  faqFormRef.current = faqForm
  editingFaqIdRef.current = editingFaqId
  siteContentFormRef.current = siteContentForm
  editingSiteContentIdRef.current = editingSiteContentId
  workScenarioFormRef.current = workScenarioForm
  editingWorkScenarioIdRef.current = editingWorkScenarioId
  vpnPanelFormRef.current = vpnPanelForm
  editingVpnPanelIdRef.current = editingVpnPanelId
  inboundFormRef.current = inboundForm
  editingInboundIdRef.current = editingInboundId
  serverFormRef.current = serverForm
  editingServerIdRef.current = editingServerId
  botSettingsFormRef.current = botSettingsForm
  const renderSessionOperationId = sessionOperationId.current
  const availableAdminSections = useMemo(
    () => adminSession ? adminSections.filter(([id]) => canAccessAdminSection(adminSession.capabilities, id)) : [],
    [adminSession]
  )
  const availableAdminSectionIds = useMemo(() => new Set(availableAdminSections.map(([id]) => id)), [availableAdminSections])
  const activeSectionLabel = adminSectionLabel(activeSection)
  const activeSectionDescription = adminSectionDescriptions[activeSection]
  const activeSectionLoadErrors = loadErrors.filter((item) => adminSectionLoadAreas[activeSection].includes(item.area))
  const activeSectionLoadFailed = activeSectionLoadErrors.length > 0
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
  const adminAccessNow = useMemo(() => new Date(), [accessCredentials, adminAccessClockTick, subscriptions, userOverview])

  const derivedSummary = useMemo(() => ({
    totalUsers: summary?.totalUsers ?? users.length,
    telegramUsers: summary?.telegramUsers ?? users.filter((item) => s(item.authSource, '') === 'Telegram').length,
    activeSubscriptions: summary?.activeSubscriptions ?? subscriptions.filter((item) => getAdminSubscriptionActionAvailability(item, adminAccessNow).isAccessAvailable).length,
    expiringSubscriptions: summary?.expiringSubscriptions ?? subscriptions.filter((item) => {
      const availability = getAdminSubscriptionActionAvailability(item, adminAccessNow)
      return availability.isAccessAvailable
        && getAdminSubscriptionEffectiveEndTime(item) <= adminAccessNow.getTime() + 7 * 24 * 60 * 60 * 1000
    }).length,
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
  }), [adminAccessNow, summary, users, subscriptions, accessCredentials, orders, payments, servers, provisioningRuns, supportConversations, vpnPanels])
  const dashboardFailedPayments = canReadFinance ? payments.filter((item) => item.status === 'Failed' || item.status === 'Cancelled').slice(0, 3) : []
  const dashboardFailedProvisioningRuns = provisioningRuns.filter((run) => ['Failed', 'PrecheckFailed'].includes(run.status)).slice(0, 3)
  const dashboardOpenSupportConversations = canReadSupport ? supportConversations.filter((conversation) => conversation.status !== 'closed').slice(0, 3) : []
  const userOverviewStats = useMemo(() => buildAdminUserOverviewStats(userOverview, adminAccessNow), [adminAccessNow, userOverview])
  const userEditErrors = useMemo(() => validateAdminUserEditForm(userEditForm), [userEditForm])
  const userEditChanged = Boolean(userOverview && isAdminUserEditFormChanged(userEditForm, userOverview.user))
  const userEditRevokesSessions = Boolean(userOverview
    && !userOverview.user.isBlocked
    && userOverview.user.status === 'Active'
    && (userEditForm.isBlocked || userEditForm.status !== 'Active'))
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
      errors.push({ area, message: normalizeApiError(e, 'Не удалось загрузить данные раздела.') })
      return fallback
    }
  }

  const selectAdminUser = (userId: string) => {
    if (selectedUserIdRef.current === userId) return
    selectedUserIdRef.current = userId
    userOverviewRequestId.current += 1
    userOverviewLoadInFlight.current = null
    setUserOverview(null)
    setUserEditForm(defaultAdminUserEditForm)
    setUserOverviewLoading(false)
    setUserOverviewError('')
    setSelectedUserId(userId)
  }

  const selectSupportConversation = (conversationId: string) => {
    if (selectedSupportConversationIdRef.current === conversationId) return
    selectedSupportConversationIdRef.current = conversationId
    supportMessagesRequestId.current += 1
    supportMessagesLoadInFlight.current = null
    setSupportMessages([])
    setSupportMessagesLoading(false)
    setSupportMessagesError('')
    setSupportReplyText('')
    setSupportNoteText('')
    setSelectedSupportConversationId(conversationId)
  }

  const selectVpnPanel = (panelId: string) => {
    if (selectedVpnPanelIdRef.current === panelId) return
    selectedVpnPanelIdRef.current = panelId
    clearVpnPanelDetails()
    setSelectedVpnPanelId(panelId)
  }

  const loadUsers = (currentToken = token, operationId = renderSessionOperationId): Promise<boolean> => {
    if (!currentToken || sessionOperationId.current !== operationId) return Promise.resolve(false)
    const search = userSearch.trim()
    const status = userStatusFilter
    const activeRequest = usersLoadInFlight.current
    if (activeRequest?.operationId === operationId
      && activeRequest.token === currentToken
      && activeRequest.search === search
      && activeRequest.status === status) {
      return activeRequest.promise
    }
    const requestId = ++usersRequestId.current
    const requestIsCurrent = () => sessionOperationId.current === operationId
      && usersRequestId.current === requestId
    const request: AdminUsersLoadRequest = { operationId, token: currentToken, search, status, promise: Promise.resolve(false) }
    const promise = (async () => {
      if (requestIsCurrent()) {
        setUsersLoading(true)
        setUsersError('')
      }
      try {
        const nextUsers = await api.getAdminUsers(currentToken, { search, status })
        if (!requestIsCurrent()) return false
        setUsers(nextUsers)
        if (!nextUsers.some((item) => String(item.id) === selectedUserIdRef.current)) {
          selectAdminUser(String(nextUsers[0]?.id ?? ''))
        }
        return true
      } catch (e) {
        if (!requestIsCurrent()) return false
        setUsersError(normalizeApiError(e, 'Не удалось загрузить пользователей'))
        return false
      } finally {
        if (requestIsCurrent()) setUsersLoading(false)
        if (usersLoadInFlight.current === request) usersLoadInFlight.current = null
      }
    })()
    request.promise = promise
    usersLoadInFlight.current = request
    return promise
  }

  const loadAll = async (currentToken: string, currentSession: AdminSessionDto | null = adminSession, options?: { operationId?: number }): Promise<boolean> => {
    if (!currentToken || !currentSession) return false

    const operationId = options?.operationId ?? renderSessionOperationId
    if (sessionOperationId.current !== operationId) return false
    const activeRequest = loadAllInFlight.current
    if (activeRequest?.operationId === operationId
      && activeRequest.token === currentToken
      && activeRequest.session === currentSession) {
      return activeRequest.promise
    }
    const requestId = ++loadAllRequestId.current
    const userListRequestId = ++usersRequestId.current
    usersLoadInFlight.current = null
    setUsersLoading(false)
    setUsersError('')
    const operationIsCurrent = () => sessionOperationId.current === operationId
      && loadAllRequestId.current === requestId
    let completeRequest!: (result: boolean) => void
    const request: AdminLoadRequest = {
      operationId,
      token: currentToken,
      session: currentSession,
      promise: new Promise<boolean>((resolve) => { completeRequest = resolve })
    }
    loadAllInFlight.current = request
    if (operationIsCurrent()) {
      setBusy(true)
      setError('')
    }
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

    if (!operationIsCurrent()) {
      completeRequest(false)
      if (loadAllInFlight.current === request) loadAllInFlight.current = null
      return false
    }

    setSummary(nextSummary)
    setAuditLogs(nextAuditLogs)
    setNotificationDeliveries(nextNotificationDeliveries)
    if (usersRequestId.current === userListRequestId) setUsers(nextUsers)
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
    const nextSelectedVpnPanelId = nextVpnPanels.some((panel) => panel.id === selectedVpnPanelIdRef.current)
      ? selectedVpnPanelIdRef.current
      : nextVpnPanels[0]?.id ?? ''
    if (nextSelectedVpnPanelId !== selectedVpnPanelIdRef.current) {
      selectVpnPanel(nextSelectedVpnPanelId)
    }
    setBotSettings(nextBotSettings)
    botSettingsCheckRequestId.current += 1
    setBotSettingsCheck(null)
    if (!botSettingsFormDirty.current) {
      setBotSettingsForm(telegramBotSettingsToForm(nextBotSettings))
    }
    setLoadErrors(errors)
    setAdminDataReady(true)
    const nextSelectedSupportConversationId = nextSupportConversations.some((item) => item.id === selectedSupportConversationIdRef.current)
      ? selectedSupportConversationIdRef.current
      : String(nextSupportConversations[0]?.id ?? '')
    if (nextSelectedSupportConversationId !== selectedSupportConversationIdRef.current) {
      selectSupportConversation(nextSelectedSupportConversationId)
    }
    if (usersRequestId.current === userListRequestId && !nextUsers.some((item) => String(item.id) === selectedUserIdRef.current)) {
      selectAdminUser(String(nextUsers[0]?.id ?? ''))
    }
    setBusy(false)
    completeRequest(true)
    if (loadAllInFlight.current === request) loadAllInFlight.current = null
    return true
  }

  const verifyAdminSession = async (accessToken: string, currentRefreshToken: string, revokeOnFailure = false) => {
    try {
      return await api.getAdminSession(accessToken)
    } catch (error) {
      if (revokeOnFailure) {
        try {
          await api.logout(accessToken, currentRefreshToken || null)
        } catch {
          // Local admission still fails closed when server-side cleanup cannot be confirmed.
        }
      }
      if (!isAdminSessionRejected(error)) throw error

      const denied = error as ApiClientError
      throw new ApiClientError(adminAccessDeniedMessage, denied.status, denied.payload)
    }
  }

  const hydrateRestoredAdminSession = async (currentToken: string, currentRefreshToken: string) => {
    if (!currentToken) return false

    const operationId = ++sessionOperationId.current
    const operationIsCurrent = () => sessionOperationId.current === operationId
    let activeAccessToken = currentToken
    let activeRefreshToken = currentRefreshToken
    setSessionHydrating(true)
    setBusy(true)
    setError('')
    setNotice('')

    try {
      let verifiedSession: AdminSessionDto
      try {
        verifiedSession = await verifyAdminSession(activeAccessToken, activeRefreshToken)
      } catch (error) {
        if (!operationIsCurrent()) return false
        if (!activeRefreshToken || !isAdminAccessTokenExpired(error)) throw error

        const response = await api.refresh(activeRefreshToken)
        if (!operationIsCurrent()) return false

        activeAccessToken = response.accessToken
        activeRefreshToken = response.refreshToken
        writeSessionStorageItem(TOKEN_STORAGE_KEY, activeAccessToken)
        writeSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY, activeRefreshToken)
        setToken(activeAccessToken)
        setRefreshToken(activeRefreshToken)
        verifiedSession = await verifyAdminSession(activeAccessToken, activeRefreshToken)
      }

      if (!operationIsCurrent()) return false
      setAdminSession(verifiedSession)
      return await loadAll(activeAccessToken, verifiedSession, { operationId })
    } catch (error) {
      if (!operationIsCurrent()) return false
      if (isAdminSessionRejected(error)) {
        const revokeRequest = api.logout(activeAccessToken || null, activeRefreshToken || null).catch(() => undefined)
        clearAdminSession()
        setError(error instanceof ApiClientError && error.status === 403 ? adminAccessDeniedMessage : adminSessionEndedMessage)
        await revokeRequest
      } else {
        clearAdminData()
        setError(normalizeApiError(error, 'Не удалось восстановить сессию администратора'))
      }
      return false
    } finally {
      if (operationIsCurrent()) {
        setSessionHydrating(false)
        setBusy(false)
      }
    }
  }

  useEffect(() => {
    if (restoredSessionHydrationStarted.current) return
    restoredSessionHydrationStarted.current = true
    void hydrateRestoredAdminSession(token, refreshToken)
  }, [])

  useEffect(() => {
    if (!adminSession || availableAdminSections.length === 0 || availableAdminSectionIds.has(activeSection)) return
    const fallbackSection = availableAdminSections[0][0]
    setActiveSection(fallbackSection)
    window.history.replaceState(null, '', `#${fallbackSection}`)
    window.requestAnimationFrame(() => document.getElementById('admin-content')?.focus())
  }, [activeSection, adminSession, availableAdminSectionIds, availableAdminSections])

  useEffect(() => {
    const metadata = getAdminPageMetadata({
      sectionLabel: activeSectionLabel,
      sectionDescription: activeSectionDescription,
      hasAdminSession: Boolean(adminSession),
      sessionHydrating
    })
    document.title = metadata.title
    const description = document.querySelector('meta[name="description"]') ?? document.head.appendChild(document.createElement('meta'))
    description.setAttribute('name', 'description')
    description.setAttribute('content', metadata.description)
  }, [activeSectionDescription, activeSectionLabel, adminSession, sessionHydrating])

  useEffect(() => {
    if (!adminSession) return
    const focusTargetId = adminDataReady ? 'admin-content' : 'admin-data-loading'
    window.requestAnimationFrame(() => document.getElementById(focusTargetId)?.focus())
  }, [adminDataReady, adminSession])

  useEffect(() => {
    let focusFrame = 0
    const syncActiveSection = (focusContent = false) => {
      const nextSection = readAdminSectionFromHash()
      if (window.location.hash && parseAdminSectionHref(window.location.hash) === null) {
        window.history.replaceState(null, '', `#${nextSection}`)
      }
      setActiveSection(nextSection)
      if (!focusContent) return
      window.cancelAnimationFrame(focusFrame)
      focusFrame = window.requestAnimationFrame(() => document.getElementById('admin-content')?.focus())
    }
    const handleHistoryNavigation = () => syncActiveSection(true)
    syncActiveSection()
    window.addEventListener('hashchange', handleHistoryNavigation)
    window.addEventListener('popstate', handleHistoryNavigation)

    return () => {
      window.cancelAnimationFrame(focusFrame)
      window.removeEventListener('hashchange', handleHistoryNavigation)
      window.removeEventListener('popstate', handleHistoryNavigation)
    }
  }, [])

  useEffect(() => {
    setEditingInboundId(null)
    setInboundForm(defaultInboundForm)
    if (token && selectedVpnPanelId) {
      void loadVpnPanelDetails(selectedVpnPanelId, token, sessionOperationId.current)
    } else {
      clearVpnPanelDetails()
    }
  }, [token, selectedVpnPanelId])

  useEffect(() => {
    if (editingServerId && !servers.some((server) => server.id === editingServerId)) {
      setEditingServerId(null)
      setEditingServerRevision(null)
      setServerForm(defaultServerForm)
    }
  }, [servers, editingServerId])

  useEffect(() => {
    if (token && selectedSupportConversationId) {
      void loadSupportMessages(selectedSupportConversationId, token, sessionOperationId.current)
    } else {
      supportMessagesRequestId.current += 1
      setSupportMessages([])
      setSupportMessagesLoading(false)
      setSupportMessagesError('')
    }
  }, [token, selectedSupportConversationId])

  useEffect(() => {
    if (token && selectedUserId) {
      void loadUserOverview(selectedUserId, token, sessionOperationId.current)
    } else {
      userOverviewRequestId.current += 1
      setUserOverview(null)
      setUserOverviewLoading(false)
      setUserOverviewError('')
    }
  }, [token, selectedUserId])

  useEffect(() => {
    if (!token) return

    const overviewAccesses = userOverview?.accessCredentials ?? []
    const accessDelay = getNextAdminAccessExpiryDelay([...accessCredentials, ...overviewAccesses])
    const subscriptionDelay = getNextAdminSubscriptionExpiryDelay([
      ...subscriptions,
      ...(userOverview?.subscriptions ?? [])
    ])
    const delays = [accessDelay, subscriptionDelay].filter((delay): delay is number => delay !== null)
    if (delays.length === 0) return
    const nextDelay = Math.min(...delays)
    const refreshDashboardSummary = subscriptionDelay !== null && subscriptionDelay === nextDelay
    const operationId = sessionOperationId.current

    const timeoutId = window.setTimeout(() => {
      setAdminQrSvgs({})
      setAdminAccessClockTick((current) => current + 1)
      if (refreshDashboardSummary) {
        setSummary(null)
        void api.getAdminDashboardSummary(token)
          .then((nextSummary) => {
            if (sessionOperationId.current === operationId) setSummary(nextSummary)
          })
          .catch(() => undefined)
      }
    }, Math.min(nextDelay, 2_147_483_647))
    return () => window.clearTimeout(timeoutId)
  }, [accessCredentials, adminAccessClockTick, subscriptions, token, userOverview])

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
  const updateWorkScenarioTariffLink = (tariffId: string, checked: boolean) => {
    setWorkScenarioForm((current) => {
      const currentIds = parseWorkScenarioTariffIds(current.allowedTariffIdsJson)
      const nextIds = checked ? [...currentIds, tariffId] : currentIds.filter((id) => id !== tariffId)
      return { ...current, allowedTariffIdsJson: scenarioTariffIdsToJson(nextIds) }
    })
  }
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
  const updateBotForm = <K extends keyof UpdateTelegramBotSettingsPayload>(key: K, value: UpdateTelegramBotSettingsPayload[K]) => {
    botSettingsFormDirty.current = true
    setBotSettingsForm((current) => ({ ...current, [key]: value }))
  }

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
      if (parseAdminSectionHref(window.location.hash) !== sectionId) {
        window.history.pushState(null, '', `#${sectionId}`)
      }
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

  const runAction = async (
    requiredSections: AdminSectionId | readonly AdminSectionId[] | null,
    id: string,
    action: (context: AdminActionContext) => Promise<void>,
    resourceKeys: string | readonly string[] = id
  ) => {
    const sections = requiredSections === null ? [] : Array.isArray(requiredSections) ? requiredSections : [requiredSections]
    const deniedSection = sections.find((section) => !canWriteSection(section))
    if (deniedSection) {
      setError(`Роль ${adminSession?.roles.join(', ') || 'текущей сессии'} не разрешает изменять раздел «${adminSectionLabel(deniedSection)}».`)
      return
    }
    const operationId = sessionOperationId.current
    const operationIsCurrent = () => sessionOperationId.current === operationId
    const requestKey = `${operationId}:${id}`
    const ownedResourceKeys = typeof resourceKeys === 'string' ? [resourceKeys] : [...new Set(resourceKeys)]
    if (actionRequestsInFlight.current.has(requestKey) || ownedResourceKeys.some((key) => actionResourceOwners.current.has(key))) return
    actionRequestsInFlight.current.add(requestKey)
    for (const key of ownedResourceKeys) actionResourceOwners.current.set(key, requestKey)
    setActionBusyResourceKeys((current) => {
      const next = new Set(current)
      for (const key of ownedResourceKeys) next.add(key)
      return next
    })
    setError('')
    setNotice('')
    try {
      await action({
        operationId,
        isCurrent: operationIsCurrent,
        reloadAll: () => operationIsCurrent()
          ? loadAll(token, adminSession, { operationId })
          : Promise.resolve(false)
      })
    } catch (e) {
      if (operationIsCurrent()) setError(normalizeApiError(e, 'Не удалось выполнить действие. Повторите попытку.'))
    } finally {
      actionRequestsInFlight.current.delete(requestKey)
      const releasedResourceKeys = ownedResourceKeys.filter((key) => actionResourceOwners.current.get(key) === requestKey)
      if (releasedResourceKeys.length > 0) {
        for (const key of releasedResourceKeys) actionResourceOwners.current.delete(key)
        setActionBusyResourceKeys((current) => {
          const next = new Set(current)
          for (const key of releasedResourceKeys) next.delete(key)
          return next
        })
      }
    }
  }

  const clearAdminData = () => {
    loadAllRequestId.current += 1
    usersRequestId.current += 1
    usersLoadInFlight.current = null
    setAdminSession(null)
    setAdminDataReady(false)
    setPassword('')
    setUsers([])
    setUsersLoading(false)
    setUsersError('')
    selectedUserIdRef.current = ''
    setSelectedUserId('')
    setUserOverview(null)
    setUserEditForm(defaultAdminUserEditForm)
    userOverviewRequestId.current += 1
    userOverviewLoadInFlight.current = null
    setUserOverviewLoading(false)
    setUserOverviewError('')
    setUserSearch('')
    setUserStatusFilter('')
    setSummary(null)
    setAuditLogs([])
    setAuditActionFilter('')
    setAuditEntityTypeFilter('')
    setAuditActorTypeFilter('')
    setAuditSearch('')
    setNotificationDeliveries([])
    setSubscriptions([])
    setAccessCredentials([])
    setAdminQrSvgs({})
    setOrders([])
    setOrderStatusFilter('all')
    setOrderSearch('')
    setPayments([])
    setRefundAmounts({})
    setRefundReasons({})
    setPaymentProviderAccounts([])
    setProviderCheckResults({})
    setPaymentWebhookEvents([])
    setRefunds([])
    setSupportConversations([])
    selectedSupportConversationIdRef.current = ''
    setSelectedSupportConversationId('')
    setSupportMessages([])
    supportMessagesRequestId.current += 1
    supportMessagesLoadInFlight.current = null
    setSupportMessagesLoading(false)
    setSupportMessagesError('')
    setSupportReplyText('')
    setSupportNoteText('')
    setTariffs([])
    setTariffForm(defaultTariffForm)
    setTariffFeaturesText('')
    setEditingTariffId('')
    setReferralPrograms([])
    setReferralRewards([])
    setReferralProgramForm(defaultReferralProgramForm)
    setEditingReferralProgramId('')
    setEditingProviderAccountId('')
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
    setEditingServerRevision(null)
    setProvisioningRuns([])
    setVpnPanels([])
    selectedVpnPanelIdRef.current = ''
    setSelectedVpnPanelId('')
    setVpnInbounds([])
    setVpnMigrationInbounds([])
    setVpnClients([])
    setVpnClientMigrationTargets({})
    setSubscriptionMigrationTargets({})
    setVpnHealthChecks([])
    setVpnSyncRuns([])
    vpnPanelDetailsRequestId.current += 1
    vpnPanelDetailsLoadInFlight.current = null
    setVpnPanelDetailsLoading(false)
    setVpnPanelDetailsError('')
    setVpnPanelForm(defaultVpnPanelForm)
    setEditingVpnPanelId(null)
    setInboundForm(defaultInboundForm)
    setEditingInboundId(null)
    setSubscriptionExtendDays({})
    setProviderForm(defaultProviderForm)
    setBotSettings(defaultBotSettings)
    botSettingsCheckRequestId.current += 1
    botSettingsFormDirty.current = false
    setBotSettingsForm({})
    setBotSettingsCheck(null)
    setLoadErrors([])
    actionRequestsInFlight.current.clear()
    actionResourceOwners.current.clear()
    setActionBusyResourceKeys(new Set())
  }

  const clearAdminSession = () => {
    sessionOperationId.current += 1
    loadAllInFlight.current = null
    loginRequestInFlight.current = null
    refreshSessionRequestInFlight.current = null
    logoutRequestInFlight.current = null
    removeSessionStorageItem(TOKEN_STORAGE_KEY)
    removeSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY)
    setToken('')
    setRefreshToken('')
    setSessionHydrating(false)
    setBusy(false)
    clearAdminData()
  }

  const handleLogin = (): Promise<void> => {
    const activeRequest = loginRequestInFlight.current
    if (activeRequest && sessionOperationId.current === activeRequest.operationId) return activeRequest.promise
    const validationErrors = validateAdminLogin(email, password)
    if (validationErrors.length > 0) {
      setError(validationErrors.join(' '))
      return Promise.resolve()
    }

    const normalizedEmail = email.trim()
    const submittedPassword = password
    const operationId = ++sessionOperationId.current
    const operationIsCurrent = () => sessionOperationId.current === operationId
    const request: AdminSessionCommandRequest = { operationId, key: normalizedEmail, promise: Promise.resolve() }
    const promise = (async () => {
      setBusy(true)
      setError('')
      setNotice('')
      try {
        const response = await api.login(normalizedEmail, submittedPassword)
        if (!operationIsCurrent()) return
        const verifiedSession = await verifyAdminSession(response.accessToken, response.refreshToken, true)
        if (!operationIsCurrent()) return
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
        await loadAll(response.accessToken, verifiedSession, { operationId })
      } catch (e) {
        if (!operationIsCurrent()) return
        setError(normalizeApiError(e, 'Не удалось получить admin token'))
      } finally {
        if (operationIsCurrent()) setBusy(false)
        if (loginRequestInFlight.current === request) loginRequestInFlight.current = null
      }
    })()
    request.promise = promise
    loginRequestInFlight.current = request
    return promise
  }

  const handleRefreshSession = (): Promise<void> => {
    const activeRequest = refreshSessionRequestInFlight.current
    if (activeRequest
      && sessionOperationId.current === activeRequest.operationId
      && activeRequest.key === refreshToken) {
      return activeRequest.promise
    }
    if (!refreshToken) {
      setError('Сессия не найдена. Войдите заново.')
      return Promise.resolve()
    }

    const submittedAccessToken = token
    const submittedRefreshToken = refreshToken
    const operationId = ++sessionOperationId.current
    const operationIsCurrent = () => sessionOperationId.current === operationId
    const request: AdminSessionCommandRequest = { operationId, key: submittedRefreshToken, promise: Promise.resolve() }
    const promise = (async () => {
      let activeAccessToken = submittedAccessToken
      let activeRefreshToken = submittedRefreshToken
      setBusy(true)
      setError('')
      setNotice('')
      let refreshRotated = false
      try {
        const response = await api.refresh(submittedRefreshToken)
        if (!operationIsCurrent()) return
        refreshRotated = true
        activeAccessToken = response.accessToken
        activeRefreshToken = response.refreshToken
        writeSessionStorageItem(TOKEN_STORAGE_KEY, activeAccessToken)
        writeSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY, activeRefreshToken)
        setToken(activeAccessToken)
        setRefreshToken(activeRefreshToken)
        const verifiedSession = await verifyAdminSession(activeAccessToken, activeRefreshToken)
        if (!operationIsCurrent()) return
        setAdminSession(verifiedSession)
        await loadAll(activeAccessToken, verifiedSession, { operationId })
        setNotice('Сессия администратора обновлена.')
      } catch (e) {
        if (!operationIsCurrent()) return
        if (isAdminSessionRejected(e)) {
          const revokeRequest = api.logout(activeAccessToken || null, activeRefreshToken || null).catch(() => undefined)
          clearAdminSession()
          setError(e instanceof ApiClientError && e.status === 403 ? adminAccessDeniedMessage : adminSessionEndedMessage)
          await revokeRequest
        } else if (refreshRotated) {
          clearAdminData()
          setSessionHydrating(false)
          setError(normalizeApiError(e, 'Не удалось проверить обновлённую сессию администратора'))
        } else {
          setError(normalizeApiError(e, 'Не удалось обновить сессию администратора'))
        }
      } finally {
        if (operationIsCurrent()) setBusy(false)
        if (refreshSessionRequestInFlight.current === request) refreshSessionRequestInFlight.current = null
      }
    })()
    request.promise = promise
    refreshSessionRequestInFlight.current = request
    return promise
  }

  const handleLogout = (): Promise<void> => {
    const activeRequest = logoutRequestInFlight.current
    if (activeRequest && sessionOperationId.current === activeRequest.operationId) return activeRequest.promise
    const submittedAccessToken = token
    const submittedRefreshToken = refreshToken
    const operationId = ++sessionOperationId.current
    const request: AdminSessionCommandRequest = {
      operationId,
      key: `${submittedAccessToken}\u0000${submittedRefreshToken}`,
      promise: Promise.resolve()
    }
    const promise = (async () => {
      setSessionHydrating(false)
      setLogoutBusy(true)
      setBusy(true)
      setError('')
      setNotice('')
      let logoutFailed = false
      try {
        await api.logout(submittedAccessToken || null, submittedRefreshToken || null)
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
        setLogoutBusy(false)
        if (logoutRequestInFlight.current === request) logoutRequestInFlight.current = null
      }
    })()
    request.promise = promise
    logoutRequestInFlight.current = request
    return promise
  }

  const loadUserOverview = (
    userId: string,
    currentToken = token,
    operationId = sessionOperationId.current
  ): Promise<boolean> => {
    if (!currentToken || !userId || sessionOperationId.current !== operationId || selectedUserIdRef.current !== userId) return Promise.resolve(false)
    const activeRequest = userOverviewLoadInFlight.current
    if (activeRequest?.operationId === operationId
      && activeRequest.token === currentToken
      && activeRequest.entityId === userId) {
      return activeRequest.promise
    }
    const requestId = ++userOverviewRequestId.current
    const requestIsCurrent = () => sessionOperationId.current === operationId
      && userOverviewRequestId.current === requestId
      && selectedUserIdRef.current === userId
    const request: AdminDetailLoadRequest = { operationId, token: currentToken, entityId: userId, promise: Promise.resolve(false) }
    const promise = (async () => {
      setUserOverview(null)
      setUserOverviewLoading(true)
      setUserOverviewError('')
      try {
        const overview = await api.getAdminUserOverview(currentToken, userId)
        if (!requestIsCurrent()) return false
        setUserOverview(overview)
        setUserEditForm(adminUserToEditForm(overview.user))
        setUserOverviewError('')
        return true
      } catch (e) {
        if (!requestIsCurrent()) return false
        setUserOverviewError(normalizeApiError(e, 'Не удалось загрузить карточку пользователя'))
        return false
      } finally {
        if (requestIsCurrent()) setUserOverviewLoading(false)
        if (userOverviewLoadInFlight.current === request) userOverviewLoadInFlight.current = null
      }
    })()
    request.promise = promise
    userOverviewLoadInFlight.current = request
    return promise
  }

  const handleSaveAdminUser = () => {
    const currentUser = userOverview?.user
    if (!currentUser || selectedUserIdRef.current !== currentUser.id) return Promise.resolve()
    const submittedForm = { ...userEditForm, displayName: userEditForm.displayName.trim() }
    const validationErrors = validateAdminUserEditForm(submittedForm)
    if (validationErrors.length > 0) {
      setError(`Пользователь: ${validationErrors.join(' ')}`)
      return Promise.resolve()
    }
    if (!isAdminUserEditFormChanged(submittedForm, currentUser)) {
      setError('Пользователь: изменения профиля не обнаружены.')
      return Promise.resolve()
    }

    const payload: UpdateAdminUserPayload = {
      displayName: submittedForm.displayName,
      status: submittedForm.status as UpdateAdminUserPayload['status'],
      isBlocked: submittedForm.isBlocked
    }
    return runAction('users', `user:update:${currentUser.id}:${currentUser.updatedAt}`, async ({ isCurrent }) => {
      let saved: AdminUserDto
      try {
        saved = await api.updateAdminUser(token, currentUser.id, payload, currentUser.updatedAt)
      } catch (actionError) {
        if (actionError instanceof ApiClientError
          && actionError.status === 409
          && isCurrent()
          && selectedUserIdRef.current === currentUser.id) {
          await loadUserOverview(currentUser.id, token, sessionOperationId.current)
        }
        throw actionError
      }
      if (!isCurrent() || selectedUserIdRef.current !== currentUser.id) return
      setUsers((items) => items.map((item) => item.id === saved.id ? saved : item))
      setUserOverview((overview) => overview?.user.id === saved.id ? { ...overview, user: saved } : overview)
      setUserEditForm(adminUserToEditForm(saved))
      setNotice(`Профиль пользователя ${saved.displayName} обновлён.`)
    }, userActionResourceKey(currentUser.id))
  }

  const loadSupportMessages = (
    conversationId: string,
    currentToken = token,
    operationId = sessionOperationId.current
  ): Promise<boolean> => {
    if (!currentToken || !conversationId || sessionOperationId.current !== operationId || selectedSupportConversationIdRef.current !== conversationId) return Promise.resolve(false)
    const activeRequest = supportMessagesLoadInFlight.current
    if (activeRequest?.operationId === operationId
      && activeRequest.token === currentToken
      && activeRequest.entityId === conversationId) {
      return activeRequest.promise
    }
    const requestId = ++supportMessagesRequestId.current
    const requestIsCurrent = () => sessionOperationId.current === operationId
      && supportMessagesRequestId.current === requestId
      && selectedSupportConversationIdRef.current === conversationId
    const request: AdminDetailLoadRequest = { operationId, token: currentToken, entityId: conversationId, promise: Promise.resolve(false) }
    const promise = (async () => {
      setSupportMessages([])
      setSupportMessagesLoading(true)
      setSupportMessagesError('')
      try {
        const messages = await api.getAdminSupportMessages(currentToken, conversationId)
        if (!requestIsCurrent()) return false
        setSupportMessages(messages)
        setSupportMessagesError('')
        return true
      } catch (e) {
        if (!requestIsCurrent()) return false
        setSupportMessagesError(normalizeApiError(e, 'Не удалось загрузить сообщения поддержки'))
        return false
      } finally {
        if (requestIsCurrent()) setSupportMessagesLoading(false)
        if (supportMessagesLoadInFlight.current === request) supportMessagesLoadInFlight.current = null
      }
    })()
    request.promise = promise
    supportMessagesLoadInFlight.current = request
    return promise
  }

  const clearVpnPanelDetails = () => {
    vpnPanelDetailsRequestId.current += 1
    vpnPanelDetailsLoadInFlight.current = null
    setVpnInbounds([])
    setVpnMigrationInbounds([])
    setVpnClients([])
    setVpnClientMigrationTargets({})
    setVpnHealthChecks([])
    setVpnSyncRuns([])
    setVpnPanelDetailsLoading(false)
    setVpnPanelDetailsError('')
  }

  const loadVpnPanelDetails = (
    panelId: string,
    currentToken = token,
    operationId = renderSessionOperationId
  ): Promise<boolean> => {
    if (!currentToken || !panelId || sessionOperationId.current !== operationId || selectedVpnPanelIdRef.current !== panelId) return Promise.resolve(false)
    const activeRequest = vpnPanelDetailsLoadInFlight.current
    if (activeRequest?.operationId === operationId
      && activeRequest.token === currentToken
      && activeRequest.entityId === panelId) {
      return activeRequest.promise
    }
    const requestId = ++vpnPanelDetailsRequestId.current
    const requestIsCurrent = () => sessionOperationId.current === operationId
      && requestId === vpnPanelDetailsRequestId.current
      && selectedVpnPanelIdRef.current === panelId
    const request: AdminDetailLoadRequest = { operationId, token: currentToken, entityId: panelId, promise: Promise.resolve(false) }
    const promise = (async () => {
      setVpnPanelDetailsLoading(true)
      setVpnPanelDetailsError('')
      try {
        const [nextInbounds, nextMigrationInbounds, nextClients, nextHealthChecks, nextSyncRuns] = await Promise.all([
          api.getAdminVpnPanelInbounds(currentToken, panelId),
          api.getAdminVpnInbounds(currentToken),
          api.getAdminVpnPanelClients(currentToken, panelId),
          api.getAdminVpnPanelHealthChecks(currentToken, panelId),
          api.getAdminVpnPanelSyncRuns(currentToken, panelId)
        ])
        if (!requestIsCurrent()) return false
        setVpnInbounds(nextInbounds)
        setVpnMigrationInbounds(nextMigrationInbounds)
        setVpnClients(nextClients)
        setVpnClientMigrationTargets((current) => {
          const next: Record<string, string> = {}
          for (const client of nextClients) {
            const currentTarget = current[client.id]
            next[client.id] = currentTarget && nextMigrationInbounds.some((inbound) => inbound.id === currentTarget) ? currentTarget : ''
          }
          return next
        })
        setVpnHealthChecks(nextHealthChecks)
        setVpnSyncRuns(nextSyncRuns)
        setVpnPanelDetailsError('')
        return true
      } catch (e) {
        if (!requestIsCurrent()) return false
        clearVpnPanelDetails()
        setVpnPanelDetailsError(normalizeApiError(e, 'Не удалось загрузить детали VPN-панели'))
        return false
      } finally {
        if (requestIsCurrent()) setVpnPanelDetailsLoading(false)
        if (vpnPanelDetailsLoadInFlight.current === request) vpnPanelDetailsLoadInFlight.current = null
      }
    })()
    request.promise = promise
    vpnPanelDetailsLoadInFlight.current = request
    return promise
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
    goToAdminSection('payments')
  }

  const handleSaveProviderAccount = async () => {
    if (!token || !canWriteActiveSection) return
    const validationErrors = validatePaymentProviderForm(providerForm, providerSetup(providerForm.provider), editingProviderAccountId
      ? paymentProviderAccounts.find((account) => account.id === editingProviderAccountId)
      : undefined)
    if (validationErrors.length > 0) {
      setError(validationErrors[0])
      return
    }
    const editingId = editingProviderAccountId
    const submittedForm = providerForm
    await runAction('payments', 'provider-save', async (action) => {
      const saved = editingId
        ? await api.updateAdminPaymentProviderAccount(token, editingId, submittedForm)
        : await api.createAdminPaymentProviderAccount(token, submittedForm)
      if (!action.isCurrent()) return
      setNotice(`Способ оплаты ${saved.name} ${editingId ? 'обновлен' : 'сохранен'}. Секреты не отображаются.`)
      if (providerFormRef.current === submittedForm && editingProviderAccountIdRef.current === editingId) resetProviderForm()
      await action.reloadAll()
    }, paymentProviderActionResourceKey(editingId || 'create'))
  }

  const handleSetProviderEnabled = async (account: PaymentProviderAccountDto, enabled: boolean) => {
    await runAction('payments', account.id, async (action) => {
      await api.setAdminPaymentProviderAccountEnabled(token, account.id, enabled)
      if (!action.isCurrent()) return
      setNotice(`${account.name}: ${enabled ? 'включен' : 'выключен'}`)
      await action.reloadAll()
    }, paymentProviderActionResourceKey(account.id))
  }

  const handleCheckProviderAccount = async (account: PaymentProviderAccountDto) => {
    await runAction('payments', `provider-check-${account.id}`, async (action) => {
      const result = await api.checkAdminPaymentProviderAccount(token, account.id)
      if (!action.isCurrent()) return
      setProviderCheckResults((current) => ({ ...current, [account.id]: result }))
      setPaymentProviderAccounts((current) => current.map((item) => item.id === account.id ? result.account : item))
      setNotice(`${account.name}: ${result.isReady ? 'настройки готовы' : 'в настройках найдены проблемы'}.`)
    }, paymentProviderActionResourceKey(account.id))
  }

  const handleRecheckPayment = async (paymentId: string) => runAction('payments', paymentId, async (action) => {
    const target = payments.find((payment) => payment.id === paymentId)
    const blocker = target ? getAdminPaymentRecheckBlocker(target) : 'Платеж не найден в текущем списке.'
    if (blocker) {
      setError(blocker)
      return
    }
    const payment = await api.recheckAdminPayment(token, paymentId)
    if (!action.isCurrent()) return
    setNotice(`Платеж ${shortId(payment.paymentId)} проверен: ${formatStatusLabel(payment.status)}`)
    await action.reloadAll()
  }, paymentActionResourceKeys(paymentId, payments.find((payment) => payment.id === paymentId)?.orderId))

  const handleRecheckOrderPayment = async (order: OrderDto) => runAction('payments', `order-recheck-${order.id}`, async (action) => {
    const blocker = getAdminOrderPaymentRecheckBlocker(order)
    if (blocker) {
      setError(blocker)
      return
    }
    const payment = await api.recheckAdminOrderPayment(token, order.id)
    if (!action.isCurrent()) return
    setNotice(`Заказ ${shortId(order.id)}: последний платеж ${shortId(payment.paymentId)} проверен, статус ${formatStatusLabel(payment.status)}.`)
    await action.reloadAll()
  }, [
    orderActionResourceKey(order.id),
    ...(order.lastPaymentId ? [paymentActionResourceKey(order.lastPaymentId)] : [])
  ])

  const openOrderUser = (order: OrderDto) => {
    selectAdminUser(order.userId)
    goToAdminSection('users')
    setNotice(`Открыта карточка пользователя ${order.userEmail || shortId(order.userId)} по заказу ${shortId(order.id)}.`)
  }

  const openOrderPayment = (order: OrderDto) => {
    goToAdminSection('payments')
    if (order.lastPaymentId && typeof window !== 'undefined' && typeof document !== 'undefined') {
      window.setTimeout(() => document.getElementById(`payment-${order.lastPaymentId}`)?.scrollIntoView({ block: 'center', behavior: 'smooth' }), 0)
    }
    setNotice(order.lastPaymentId ? `Открыт связанный платеж ${shortId(order.lastPaymentId)}.` : 'У заказа пока нет платежной попытки.')
  }

  const openOrderSubscription = (order: OrderDto) => {
    goToAdminSection('subscriptions')
    if (order.linkedSubscriptionId && typeof window !== 'undefined' && typeof document !== 'undefined') {
      window.setTimeout(() => document.getElementById(`subscription-${order.linkedSubscriptionId}`)?.scrollIntoView({ block: 'center', behavior: 'smooth' }), 0)
    }
    setNotice(order.linkedSubscriptionId ? `Открыта связанная подписка ${shortId(order.linkedSubscriptionId)}.` : 'У заказа нет связанной подписки.')
  }

  const handleRefundPayment = async (payment: PaymentAttemptDto) => {
    await runAction('payments', payment.id, async (action) => {
      const amount = refundAmounts[payment.id] ?? getRefundableAmount(payment)
      const reason = refundReasons[payment.id]?.trim() || 'manual_admin_refund'
      let refund: RefundDto
      try {
        refund = await api.refundAdminPayment(token, payment.id, amount, reason)
      } catch (error) {
        await action.reloadAll()
        throw error
      }
      if (!action.isCurrent()) return
      setNotice(`Возврат ${refund.providerRefundId || refund.id}: ${formatStatusLabel(refund.status)}`)
      setRefundAmounts((current) => {
        const next = { ...current }
        delete next[payment.id]
        return next
      })
      setRefundReasons((current) => ({ ...current, [payment.id]: '' }))
      await action.reloadAll()
    }, paymentActionResourceKeys(payment.id, payment.orderId))
  }

  const handleRecheckRefund = async (refund: RefundDto) => {
    await runAction('payments', `refund-recheck-${refund.id}`, async (action) => {
      const blocker = refundRecheckBlockerText(refund)
      if (blocker) {
        setError(blocker)
        return
      }
      const result = await api.recheckAdminRefund(token, refund.id)
      if (!action.isCurrent()) return
      setNotice(`Возврат ${result.providerRefundId || shortId(result.id)} проверен: ${formatStatusLabel(result.status)}`)
      await action.reloadAll()
    }, refundActionResourceKeys(refund))
  }

  const handleRetryRefund = async (refund: RefundDto) => {
    await runAction('payments', `refund-retry-${refund.id}`, async (action) => {
      const blocker = refundRetryBlockerText(refund)
      if (blocker) {
        setError(blocker)
        return
      }
      let result: RefundDto
      try {
        result = await api.refundAdminPayment(token, refund.paymentAttemptId, refund.amount, refund.reason)
      } catch (error) {
        await action.reloadAll()
        throw error
      }
      if (!action.isCurrent()) return
      setNotice(`Возврат ${result.providerRefundId || shortId(result.id)} повторён: ${formatStatusLabel(result.status)}`)
      await action.reloadAll()
    }, refundActionResourceKeys(refund))
  }

  const resetTariffForm = () => {
    setTariffForm(defaultTariffForm)
    setTariffFeaturesText('')
    setEditingTariffId('')
    setEditingTariffRevision(null)
  }

  const throwTariffConflict = async (
    error: unknown,
    action: AdminActionContext,
    tariffId: string,
    submittedForm?: UpdateTariffPayload,
    submittedFeaturesText?: string
  ): Promise<never> => {
    if (!(error instanceof ApiClientError) || error.status !== 409) throw error
    await action.reloadAll()
    const submittedDraftIsCurrent = submittedForm === undefined
      || (tariffFormRef.current === submittedForm && tariffFeaturesTextRef.current === submittedFeaturesText)
    if (action.isCurrent() && editingTariffIdRef.current === tariffId && submittedDraftIsCurrent) resetTariffForm()
    throw new ApiClientError('Тариф уже изменен другим администратором. Список обновлен: повторите действие с актуальной версией.', 409, error.payload)
  }

  const editTariff = (tariff: AdminTariffDto) => {
    setEditingTariffId(tariff.id)
    setEditingTariffRevision(tariff.revision)
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
      visibleFrom: tariff.visibleFrom,
      visibleTo: tariff.visibleTo,
      tariffType: tariff.tariffType,
      category: tariff.category,
      allowedRegionsCsv: tariff.allowedRegionsCsv ?? '',
      allowedNodeGroupsCsv: tariff.allowedNodeGroupsCsv ?? '',
      isReferralEligible: tariff.isReferralEligible !== false,
      provisioningScenario: tariff.provisioningScenario ?? 'auto',
      afterPaymentText: tariff.afterPaymentText ?? ''
    })
    setTariffFeaturesText(featuresToText(tariff))
    goToAdminSection('tariffs')
  }

  const handleSaveTariff = async () => {
    if (!token || !canWriteActiveSection) return
    const validationErrors = validateTariffForm(tariffForm, tariffFeaturesText)
    if (validationErrors.length > 0) {
      setError(`Тариф: ${validationErrors.join(' ')}`)
      return
    }
    const payload = normalizeTariffPayload(tariffForm, tariffFeaturesText)
    const editingId = editingTariffId
    const submittedForm = tariffForm
    const submittedFeaturesText = tariffFeaturesText
    const submittedRevision = editingTariffRevision
    const currentTariff = editingId ? tariffs.find((tariff) => tariff.id === editingId) : undefined
    if (currentTariff && !isTariffFormChanged(submittedForm, submittedFeaturesText, currentTariff)) return
    await runAction('tariffs', 'tariff-save', async (action) => {
      if (editingId) {
        if (submittedRevision === null) throw new ApiClientError('Не удалось определить ревизию тарифа. Откройте его заново.', 409, null)
        try {
          await api.updateAdminTariff(token, editingId, payload, submittedRevision)
        } catch (error) {
          await throwTariffConflict(error, action, editingId, submittedForm, submittedFeaturesText)
        }
      } else {
        await api.createAdminTariff(token, payload)
      }
      if (!action.isCurrent()) return
      setNotice(editingId ? 'Тариф обновлён.' : 'Тариф создан.')
      if (tariffFormRef.current === submittedForm
        && tariffFeaturesTextRef.current === submittedFeaturesText
        && editingTariffIdRef.current === editingId) resetTariffForm()
      await action.reloadAll()
    }, tariffActionResourceKey(editingId || 'create'))
  }

  const handleToggleTariff = async (tariff: AdminTariffDto) => {
    await runAction('tariffs', tariff.id, async (action) => {
      try {
        await api.updateAdminTariff(token, tariff.id, { isActive: tariff.isActive === false }, tariff.revision)
      } catch (error) {
        await throwTariffConflict(error, action, tariff.id)
      }
      if (!action.isCurrent()) return
      setNotice(`Тариф ${tariff.name} обновлён.`)
      await action.reloadAll()
    }, tariffActionResourceKey(tariff.id))
  }

  const handleDeleteTariff = async (tariff: AdminTariffDto) => {
    await runAction('tariffs', `delete-${tariff.id}`, async (action) => {
      const result = await api.deleteAdminTariff(token, tariff.id, tariff.revision)
        .catch((error: unknown) => throwTariffConflict(error, action, tariff.id))
      if (!action.isCurrent()) return
      if (editingTariffIdRef.current === tariff.id) resetTariffForm()
      setNotice(result.archived ? `Тариф ${tariff.name} архивирован и скрыт с витрины.` : `Тариф ${tariff.name} удалён.`)
      await action.reloadAll()
    }, tariffActionResourceKey(tariff.id))
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

    const editingId = editingReferralProgramId
    const submittedForm = referralProgramForm
    const currentProgram = editingId ? referralPrograms.find((program) => program.id === editingId) : undefined
    if (currentProgram && !isReferralProgramFormChanged(submittedForm, currentProgram)) return
    await runAction('referrals', 'referral-program-save', async (action) => {
      const payload = buildReferralProgramPayload(submittedForm)
      if (editingId) {
        try {
          await api.updateAdminReferralProgram(token, editingId, payload, submittedForm.revision)
        } catch (error) {
          if (error instanceof ApiClientError && error.status === 409) {
            await action.reloadAll()
            if (action.isCurrent() && referralProgramFormRef.current === submittedForm && editingReferralProgramIdRef.current === editingId) resetReferralProgramForm()
            throw new ApiClientError('Реферальная программа уже изменена другим администратором. Список обновлен: откройте актуальную версию и повторите правку.', 409, error.payload)
          }
          throw error
        }
      } else {
        await api.createAdminReferralProgram(token, payload)
      }
      if (!action.isCurrent()) return
      setNotice(editingId ? 'Реферальная программа обновлена.' : 'Реферальная программа создана.')
      if (referralProgramFormRef.current === submittedForm && editingReferralProgramIdRef.current === editingId) resetReferralProgramForm()
      await action.reloadAll()
    }, referralProgramActionResourceKey)
  }

  const resetReleaseForm = () => {
    setReleaseForm({ ...defaultReleaseForm, releasedAt: new Date().toISOString(), items: [{ type: 'new', text: '', sortOrder: 10 }] })
    setEditingReleaseId('')
  }

  const editRelease = (release: AdminAppReleaseDto) => {
    setEditingReleaseId(release.id)
    setReleaseForm({
      releaseId: release.releaseId,
      version: release.version,
      releasedAt: release.releasedAt,
      title: release.title,
      summary: release.summary,
      isActive: release.isActive,
      source: release.source,
      revision: release.revision,
      items: release.items.length > 0 ? release.items.map((item, index) => ({
        id: item.id ?? null,
        type: item.type,
        text: item.text,
        sortOrder: item.sortOrder || (index + 1) * 10
      })) : [{ type: 'new', text: '', sortOrder: 10 }]
    })
    goToAdminSection('releases')
  }

  const handleSaveRelease = async () => {
    if (!token || !canWriteSection('releases')) return
    const submittedForm = releaseForm
    const editingId = editingReleaseId
    const payload = normalizeAppReleasePayload(submittedForm)

    if (!payload.releaseId || !appReleaseIdPattern.test(payload.releaseId) || payload.releaseId.length > 160) {
      setError('Release ID: lowercase kebab-case, не более 160 символов.')
      return
    }
    if (!payload.version || payload.version.length > 40 || !payload.title || payload.title.length > 200 || !payload.summary || payload.summary.length > 4000) {
      setError('Проверьте версию, заголовок и описание релиза.')
      return
    }
    if (!payload.releasedAt || Number.isNaN(Date.parse(payload.releasedAt))) {
      setError('Укажите дату публикации релиза.')
      return
    }
    const itemSortOrders = payload.items.map((item) => item.sortOrder)
    if ((payload.source && !['agent', 'manual'].includes(payload.source))
      || payload.items.length === 0
      || payload.items.length > 100
      || payload.items.some((item) => !item.text || item.text.length > 4000 || !appReleaseItemTypePattern.test(item.type) || item.sortOrder < 0)
      || new Set(itemSortOrders).size !== itemSortOrders.length) {
      setError('Проверьте каждый пункт, его тип и уникальный порядок (не более 100).')
      return
    }

    const currentRelease = editingId ? appReleases.find((release) => release.id === editingId) : undefined
    if (currentRelease && !isAppReleaseFormChanged(submittedForm, currentRelease)) return

    await runAction('releases', editingId ? `release-update-${editingId}` : 'release-create', async (action) => {
      if (editingId) {
        try {
          await api.updateAdminAppRelease(token, editingId, payload)
        } catch (error) {
          if (error instanceof ApiClientError && error.status === 409) {
            await action.reloadAll()
            if (action.isCurrent() && releaseFormRef.current === submittedForm && editingReleaseIdRef.current === editingId) resetReleaseForm()
            throw new ApiClientError('Релиз уже изменен другим администратором. Список обновлен: откройте актуальную версию и повторите правку.', 409, error.payload)
          }
          throw error
        }
      } else {
        await api.createAdminAppRelease(token, payload)
      }
      if (!action.isCurrent()) return
      setNotice(editingId ? 'Релиз обновлен.' : 'Релиз создан.')
      if (releaseFormRef.current === submittedForm && editingReleaseIdRef.current === editingId) resetReleaseForm()
      await action.reloadAll()
    }, appReleaseActionResourceKey(editingId || 'create'))
  }

  const handleDeleteRelease = async (release: AdminAppReleaseDto) => {
    await runAction('releases', `release-delete-${release.id}`, async (action) => {
      try {
        await api.deleteAdminAppRelease(token, release.id, release.revision)
      } catch (error) {
        if (error instanceof ApiClientError && error.status === 409) {
          await action.reloadAll()
          if (action.isCurrent() && editingReleaseIdRef.current === release.id) resetReleaseForm()
          throw new ApiClientError('Релиз уже изменен другим администратором и не был удален. Список обновлен.', 409, error.payload)
        }
        throw error
      }
      if (!action.isCurrent()) return
      if (editingReleaseIdRef.current === release.id) resetReleaseForm()
      setNotice(`Релиз ${release.version} удален.`)
      await action.reloadAll()
    }, appReleaseActionResourceKey(release.id))
  }

  const resetFaqForm = () => {
    setFaqForm(defaultFaqForm)
    setEditingFaqId('')
  }

  const editFaq = (entry: AdminFaqItem) => {
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
    goToAdminSection('faq')
  }

  const handleSaveFaq = async () => {
    if (!token || !canWriteSection('faq')) return
    const submittedForm = faqForm
    const editingId = editingFaqId
    const payload = normalizeFaqPayload(submittedForm)
    const current = editingId ? faqEntries.find((entry) => entry.id === editingId) : undefined

    if (!payload.question || !payload.answer) {
      setError('FAQ: заполните вопрос и ответ.')
      return
    }
    if (editingId && !current) {
      setError('Вопрос FAQ больше не найден. Обновите список.')
      return
    }
    if (current && !isFaqFormChanged(submittedForm, current)) {
      setError('Изменения вопроса FAQ не обнаружены.')
      return
    }

    await runAction('faq', editingId ? `faq-update-${editingId}` : 'faq-create', async (action) => {
      if (editingId) {
        try {
          await api.updateAdminFaq(token, editingId, payload, current!.revision)
        } catch (error) {
          if (error instanceof ApiClientError && error.status === 409) {
            const latest = await api.getAdminFaq(token)
            if (!action.isCurrent()) return
            setFaqEntries(latest)
            const refreshed = latest.find((entry) => entry.id === editingId)
            if (!refreshed) resetFaqForm()
            else if (faqFormRef.current === submittedForm && editingFaqIdRef.current === editingId) editFaq(refreshed)
            throw new ApiClientError('Вопрос уже изменен другим администратором. Форма обновлена актуальными данными.', 409, error.payload)
          }
          throw error
        }
      } else {
        await api.createAdminFaq(token, payload)
      }
      if (!action.isCurrent()) return
      setNotice(editingId ? 'Вопрос FAQ обновлен.' : 'Вопрос FAQ создан.')
      if (faqFormRef.current === submittedForm && editingFaqIdRef.current === editingId) resetFaqForm()
      await action.reloadAll()
    }, faqActionResourceKey(editingId || 'create'))
  }

  const handleDeleteFaq = async (entry: AdminFaqItem) => {
    const faqId = entry.id
    if (!faqId) return
    await runAction('faq', `faq-delete-${faqId}`, async (action) => {
      try {
        await api.deleteAdminFaq(token, faqId, entry.revision)
      } catch (error) {
        if (error instanceof ApiClientError && error.status === 409) {
          await action.reloadAll()
          if (action.isCurrent() && editingFaqIdRef.current === faqId) resetFaqForm()
          throw new ApiClientError('Вопрос уже изменен другим администратором и не был удален. Список обновлен.', 409, error.payload)
        }
        throw error
      }
      if (!action.isCurrent()) return
      if (editingFaqIdRef.current === faqId) resetFaqForm()
      setNotice('Вопрос FAQ удален.')
      await action.reloadAll()
    }, faqActionResourceKey(faqId))
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
    goToAdminSection('content')
  }

  const handleSaveSiteContent = async () => {
    if (!token || !canWriteSection('content')) return
    const submittedForm = siteContentForm
    const editingId = editingSiteContentId
    const payload = normalizeSiteContentPayload(submittedForm)
    const current = editingId ? siteContentBlocks.find((block) => block.id === editingId) : undefined
    if (!payload.key || !payload.label) {
      setError('Контент сайта: заполните ключ и название поля.')
      return
    }
    if (editingId && !current) {
      setError('Редактируемый блок больше не найден. Обновите список.')
      return
    }
    if (current && !isSiteContentFormChanged(submittedForm, current)) {
      setError('Изменения блока контента не обнаружены.')
      return
    }

    await runAction('content', editingId ? `content-update-${editingId}` : 'content-create', async (action) => {
      if (editingId) {
        try {
          await api.updateAdminSiteContent(token, editingId, payload, current!.revision)
        } catch (error) {
          if (error instanceof ApiClientError && error.status === 409) {
            const refreshed = await api.getAdminSiteContent(token)
            if (action.isCurrent()) {
              setSiteContentBlocks(refreshed)
              const latest = refreshed.find((block) => block.id === editingId)
              if (!latest) resetSiteContentForm()
              else if (siteContentFormRef.current === submittedForm && editingSiteContentIdRef.current === editingId) editSiteContent(latest)
            }
            throw new ApiClientError('Блок уже изменен другим администратором. В форму загружена актуальная версия.', 409, error.payload)
          }
          throw error
        }
      } else {
        await api.createAdminSiteContent(token, payload)
      }
      if (!action.isCurrent()) return
      setNotice(editingId ? 'Блок контента обновлен.' : 'Блок контента создан.')
      if (siteContentFormRef.current === submittedForm && editingSiteContentIdRef.current === editingId) resetSiteContentForm()
      await action.reloadAll()
    }, siteContentActionResourceKey)
  }

  const handleDeleteSiteContent = async (block: SiteContentBlockDto) => {
    await runAction('content', `content-delete-${block.id}`, async (action) => {
      try {
        await api.deleteAdminSiteContent(token, block.id, block.revision)
      } catch (error) {
        if (error instanceof ApiClientError && error.status === 409) {
          await action.reloadAll()
          if (action.isCurrent() && editingSiteContentIdRef.current === block.id) resetSiteContentForm()
          throw new ApiClientError('Блок уже изменен другим администратором и не был удален. Список обновлен.', 409, error.payload)
        }
        throw error
      }
      if (!action.isCurrent()) return
      if (editingSiteContentIdRef.current === block.id) resetSiteContentForm()
      setNotice('Блок контента удален.')
      await action.reloadAll()
    }, siteContentActionResourceKey)
  }

  const handleRestoreHomeContentDefaults = async () => {
    if (!token) return
    await runAction('content', 'content-restore-defaults', async (action) => {
      const result = await api.restoreAdminHomeContentDefaults(token)
      if (!action.isCurrent()) return
      setHomeContentReadiness(result.readiness)
      setNotice(`Главная обновлена: создано ${result.created}, восстановлено ${result.restored}.`)
      await action.reloadAll()
    }, siteContentActionResourceKey)
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
    goToAdminSection('scenarios')
  }

  const handleSaveWorkScenario = async () => {
    if (!token || !canWriteSection('scenarios')) return
    const submittedForm = workScenarioForm
    const editingId = editingWorkScenarioId
    const validationErrors = validateWorkScenarioForm(submittedForm)
    if (validationErrors.length > 0) {
      setError(`Сценарий: ${validationErrors.join(' ')}`)
      return
    }

    const payload = normalizeWorkScenarioPayload(submittedForm)
    const current = editingId ? workScenarios.find((scenario) => scenario.id === editingId) : undefined
    if (editingId && !current) {
      setError('Редактируемый сценарий больше не найден. Обновите список.')
      return
    }
    if (current && !isWorkScenarioFormChanged(submittedForm, current)) {
      setError('Изменения рабочего сценария не обнаружены.')
      return
    }

    await runAction('scenarios', editingId ? `scenario-update-${editingId}` : 'scenario-create', async (action) => {
      if (editingId) {
        try {
          await api.updateAdminWorkScenario(token, editingId, payload, current!.revision)
        } catch (error) {
          if (error instanceof ApiClientError && error.status === 409) {
            const latest = await api.getAdminWorkScenarios(token)
            if (!action.isCurrent()) return
            setWorkScenarios(latest)
            const refreshed = latest.find((scenario) => scenario.id === editingId)
            if (!refreshed) resetWorkScenarioForm()
            else if (workScenarioFormRef.current === submittedForm && editingWorkScenarioIdRef.current === editingId) editWorkScenario(refreshed)
            throw new ApiClientError('Сценарий уже изменен другим администратором. Форма обновлена актуальными данными.', 409, error.payload)
          }
          throw error
        }
      } else {
        await api.createAdminWorkScenario(token, payload)
      }
      if (!action.isCurrent()) return
      setNotice(editingId ? 'Сценарий работы обновлен.' : 'Сценарий работы создан.')
      if (workScenarioFormRef.current === submittedForm && editingWorkScenarioIdRef.current === editingId) resetWorkScenarioForm()
      await action.reloadAll()
    }, workScenarioActionResourceKey(editingId || 'create'))
  }

  const handleDeleteWorkScenario = async (scenario: WorkScenarioDto) => {
    await runAction('scenarios', `scenario-delete-${scenario.id}`, async (action) => {
      try {
        await api.deleteAdminWorkScenario(token, scenario.id, scenario.revision)
      } catch (error) {
        if (error instanceof ApiClientError && error.status === 409) {
          const latest = await api.getAdminWorkScenarios(token)
          if (!action.isCurrent()) return
          setWorkScenarios(latest)
          if (editingWorkScenarioIdRef.current === scenario.id) {
            const refreshed = latest.find((item) => item.id === scenario.id)
            if (refreshed) editWorkScenario(refreshed)
            else resetWorkScenarioForm()
          }
          throw new ApiClientError('Сценарий уже изменен другим администратором и не был удален. Список обновлен.', 409, error.payload)
        }
        throw error
      }
      if (!action.isCurrent()) return
      if (editingWorkScenarioIdRef.current === scenario.id) resetWorkScenarioForm()
      setNotice('Сценарий работы удален.')
      await action.reloadAll()
    }, workScenarioActionResourceKey(scenario.id))
  }

  const clearAdminAccessQr = (accessId: string | null | undefined) => {
    if (!accessId) return
    setAdminQrSvgs((current) => {
      if (!current[accessId]) return current
      const next = { ...current }
      delete next[accessId]
      return next
    })
  }

  const handleSubscriptionAction = async (
    subscription: SubscriptionDto,
    action: Exclude<AdminSubscriptionAction, 'migrate'>
  ) => {
    const resourceKeys = subscriptionActionResourceKeys(subscription.id, subscription.currentAccessId)
    const blocker = getAdminSubscriptionActionBlocker(subscription, action, new Date())
    if (blocker) {
      setError(blocker)
      return
    }

    if (action === 'activate') {
      await runAction('subscriptions', `${action}-${subscription.id}`, async (adminAction) => {
        clearAdminAccessQr(subscription.currentAccessId)
        await api.activateAdminSubscription(token, subscription.id, 'manual_subscription_activate')
        if (!adminAction.isCurrent()) return
        setNotice('Подписка активирована, текущий VPN-доступ включен при наличии.')
        await adminAction.reloadAll()
      }, resourceKeys)
      return
    }

    if (action === 'extend') {
      const days = Number(subscriptionExtendDays[subscription.id] ?? 30)
      if (!Number.isFinite(days) || days <= 0) {
        setError('Укажите положительное количество дней для продления подписки.')
        return
      }
      await runAction('subscriptions', `${action}-${subscription.id}`, async (adminAction) => {
        clearAdminAccessQr(subscription.currentAccessId)
        await api.extendAdminSubscription(token, subscription.id, days, 'manual_admin_extend')
        if (!adminAction.isCurrent()) return
        setNotice(`Подписка продлена на ${days} дней.`)
        await adminAction.reloadAll()
      }, resourceKeys)
      return
    }

    if (action === 'sync') {
      if (!subscription.currentAccessId) {
        setError('У подписки нет текущего VPN-доступа для синхронизации.')
        return
      }

      await runAction('subscriptions', `${action}-${subscription.id}`, async (adminAction) => {
        clearAdminAccessQr(subscription.currentAccessId)
        await api.syncAdminSubscriptionAccess(token, subscription.id, 'manual_subscription_sync')
        if (!adminAction.isCurrent()) return
        setNotice('Текущий VPN-доступ подписки синхронизирован.')
        await adminAction.reloadAll()
      }, resourceKeys)
      return
    }

    const map = {
      block: () => api.blockAdminSubscription(token, subscription.id, 'manual_admin_action'),
      unblock: () => api.unblockAdminSubscription(token, subscription.id, 'manual_admin_action'),
      cancel: () => api.cancelAdminSubscription(token, subscription.id, 'manual_admin_action')
    }
    await runAction('subscriptions', `${action}-${subscription.id}`, async (adminAction) => {
      clearAdminAccessQr(subscription.currentAccessId)
      await map[action]()
      if (!adminAction.isCurrent()) return
      setNotice(action === 'cancel'
        ? 'Подписка отменена, VPN-доступ отозван и удален с сервера.'
        : `Подписка обновлена: ${shortId(subscription.id)}`)
      await adminAction.reloadAll()
    }, resourceKeys)
  }

  const subscriptionMigrationOptions = (subscription: SubscriptionDto) => servers.filter((server) =>
    server.id !== subscription.currentServerId
    && server.status === 'Ready'
    && server.isAvailableForNewUsers
    && server.healthStatus !== 'Unhealthy'
    && server.usedCapacity < server.capacity)

  const handleMigrateSubscription = async (subscription: SubscriptionDto) => {
    const blocker = getAdminSubscriptionActionBlocker(subscription, 'migrate', new Date())
    if (blocker) {
      setError(blocker)
      return
    }

    const selectedTarget = subscriptionMigrationTargets[subscription.id]
    if (!subscription.currentServerId) {
      setError('У подписки нет исходного VPN-сервера для миграции.')
      return
    }
    if (!selectedTarget) {
      setError('Выберите целевой сервер или автоматическое распределение.')
      return
    }

    const targetNodeId = selectedTarget === 'auto' ? null : selectedTarget
    await runAction(['subscriptions', 'vpn'], `migrate-${subscription.id}`, async (action) => {
      clearAdminAccessQr(subscription.currentAccessId)
      const result = await api.migrateAdminSubscription(token, subscription.id, targetNodeId)
      if (!action.isCurrent()) return
      setSubscriptionMigrationTargets((current) => ({ ...current, [subscription.id]: '' }))
      setNotice(`Подписка перенесена на сервер ${shortId(result.targetNodeId)}. Задача ${shortId(result.migrationJobId)} завершена.`)
      await action.reloadAll()
    }, subscriptionActionResourceKeys(subscription.id, subscription.currentAccessId))
  }

  const handleAccessAction = async (access: AccessCredentialDto, enable: boolean) => {
    const blocker = getAdminAccessCommandBlocker(access, enable ? 'enable' : 'disable')
    if (blocker) {
      setError(blocker)
      return
    }

    await runAction('vpn', `${enable ? 'enable' : 'disable'}-${access.id}`, async (action) => {
      clearAdminAccessQr(access.id)
      if (enable) await api.enableAdminAccess(token, access.id, 'manual_admin_action')
      else await api.disableAdminAccess(token, access.id, 'manual_admin_action')
      if (!action.isCurrent()) return
      setNotice(`VPN-доступ ${enable ? 'включен' : 'отключен'}.`)
      await action.reloadAll()
    }, accessActionResourceKey(access.id))
  }

  const handleAccessSync = async (access: AccessCredentialDto) => {
    const blocker = getAdminAccessCommandBlocker(access, 'sync')
    if (blocker) {
      setError(blocker)
      return
    }

    await runAction('vpn', `sync-${access.id}`, async (action) => {
      clearAdminAccessQr(access.id)
      await api.syncAdminAccess(token, access.id, 'manual_admin_sync')
      if (!action.isCurrent()) return
      setNotice('VPN-доступ синхронизирован.')
      await action.reloadAll()
    }, accessActionResourceKey(access.id))
  }

  const handleAccessResetTraffic = async (access: AccessCredentialDto) => {
    const blocker = getAdminAccessCommandBlocker(access, 'reset')
    if (blocker) {
      setError(blocker)
      return
    }

    await runAction('vpn', `reset-${access.id}`, async (action) => {
      clearAdminAccessQr(access.id)
      try {
        await api.resetAdminAccessTraffic(token, access.id, 'manual_admin_reset_traffic')
        if (!action.isCurrent()) return
        setNotice('Трафик VPN-доступа сброшен.')
      } finally {
        await action.reloadAll()
      }
    }, accessActionResourceKey(access.id))
  }


  const handleAdminAccessQr = async (access: AccessCredentialDto) => {
    const blocker = getAdminAccessCommandBlocker(access, 'qr')
    if (blocker) {
      clearAdminAccessQr(access.id)
      setError(blocker)
      return
    }

    await runAction(null, `qr-${access.id}`, async (action) => {
      clearAdminAccessQr(access.id)
      const svg = await api.getAdminAccessQrSvg(token, access.id)
      if (!action.isCurrent()) return
      setAdminQrSvgs((current) => ({ ...current, [access.id]: svg }))
      setNotice('QR-код загружен. Он содержит ссылку подключения и не добавляет дополнительных секретов.')
    }, accessActionResourceKey(access.id))
  }

  const handleReplySupport = async () => {
    if (!token || !canWriteSection('support') || !selectedSupportConversationId || !supportReplyText.trim()) return
    const conversationId = selectedSupportConversationId
    const operationId = sessionOperationId.current
    const operationIsCurrent = () => sessionOperationId.current === operationId
    const conversation = supportConversations.find((item) => item.id === conversationId)
    if (!conversation) return
    await runAction('support', `support-reply-${conversationId}`, async () => {
      try {
        const result = await api.replyAdminSupportConversation(token, conversationId, supportReplyText.trim(), conversation.revision)
        if (!operationIsCurrent()) return
        setNotice(result.status === 'queued'
          ? 'Ответ сохранен и поставлен в очередь отправки Telegram.'
          : result.status === 'already_queued'
            ? 'Ответ сохранен; Telegram-доставка уже находится в очереди.'
            : 'Ответ сохранен в обращении.')
      } catch (error) {
        if (!operationIsCurrent()) return
        if (error instanceof ApiClientError && error.status === 409) {
          await loadAll(token, adminSession, { operationId })
          if (operationIsCurrent()) {
            const currentConversationId = selectedSupportConversationIdRef.current
            if (currentConversationId) await loadSupportMessages(currentConversationId, token, operationId)
          }
        }
        throw error
      }
      if (!operationIsCurrent()) return
      if (selectedSupportConversationIdRef.current === conversationId) setSupportReplyText('')
      await loadAll(token, adminSession, { operationId })
      if (operationIsCurrent() && selectedSupportConversationIdRef.current === conversationId) {
        await loadSupportMessages(conversationId, token, operationId)
      }
    }, supportActionResourceKey(conversationId))
  }

  const handleSupportStatus = async (status: string, conversationId = selectedSupportConversationId) => {
    if (!token || !canWriteSection('support') || !conversationId) return
    const operationId = sessionOperationId.current
    const operationIsCurrent = () => sessionOperationId.current === operationId
    const conversation = supportConversations.find((item) => item.id === conversationId)
    if (!conversation) return
    await runAction('support', `support-status-${conversationId}`, async () => {
      try {
        await api.updateAdminSupportConversationStatus(token, conversationId, status, conversation.revision)
      } catch (error) {
        if (!operationIsCurrent()) return
        if (error instanceof ApiClientError && error.status === 409) {
          await loadAll(token, adminSession, { operationId })
          if (operationIsCurrent()) {
            const currentConversationId = selectedSupportConversationIdRef.current
            if (currentConversationId) await loadSupportMessages(currentConversationId, token, operationId)
          }
        }
        throw error
      }
      if (!operationIsCurrent()) return
      setNotice(`Статус обращения обновлен: ${status}.`)
      await loadAll(token, adminSession, { operationId })
      if (operationIsCurrent() && selectedSupportConversationIdRef.current === conversationId) {
        await loadSupportMessages(conversationId, token, operationId)
      }
    }, supportActionResourceKey(conversationId))
  }

  const handleSupportNote = async () => {
    if (!token || !canWriteSection('support') || !selectedSupportConversationId || !supportNoteText.trim()) return
    const conversationId = selectedSupportConversationId
    const operationId = sessionOperationId.current
    const operationIsCurrent = () => sessionOperationId.current === operationId
    const conversation = supportConversations.find((item) => item.id === conversationId)
    if (!conversation) return
    await runAction('support', `support-note-${conversationId}`, async () => {
      try {
        await api.addAdminSupportInternalNote(token, conversationId, supportNoteText.trim(), conversation.revision)
      } catch (error) {
        if (!operationIsCurrent()) return
        if (error instanceof ApiClientError && error.status === 409) {
          await loadAll(token, adminSession, { operationId })
          if (operationIsCurrent()) {
            const currentConversationId = selectedSupportConversationIdRef.current
            if (currentConversationId) await loadSupportMessages(currentConversationId, token, operationId)
          }
        }
        throw error
      }
      if (!operationIsCurrent()) return
      if (selectedSupportConversationIdRef.current === conversationId) setSupportNoteText('')
      setNotice('Внутренняя заметка сохранена.')
      await loadAll(token, adminSession, { operationId })
      if (operationIsCurrent() && selectedSupportConversationIdRef.current === conversationId) {
        await loadSupportMessages(conversationId, token, operationId)
      }
    }, supportActionResourceKey(conversationId))
  }

  const editVpnPanel = (panel: VpnPanelDto) => {
    setEditingVpnPanelId(panel.id)
    selectVpnPanel(panel.id)
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
      defaultInboundTemplateJson: panel.defaultInboundTemplateJson || '{}',
      revision: panel.revision
    })
  }

  const cancelVpnPanelEdit = () => {
    setEditingVpnPanelId(null)
    setVpnPanelForm(defaultVpnPanelForm)
  }

  const throwVpnPanelConflict = async (
    error: unknown,
    action: AdminActionContext,
    draft?: VpnConflictDraft
  ): Promise<never> => {
    if (!(error instanceof ApiClientError) || error.status !== 409) throw error
    await action.reloadAll()
    if (action.isCurrent()) {
      if (draft?.kind === 'panel') {
        const submittedDraftIsCurrent = vpnPanelFormRef.current === draft.form
          && editingVpnPanelIdRef.current === draft.id
          && selectedVpnPanelIdRef.current === draft.panelId
        if (submittedDraftIsCurrent) cancelVpnPanelEdit()
      } else if (draft?.kind === 'inbound') {
        const submittedDraftIsCurrent = inboundFormRef.current === draft.form
          && editingInboundIdRef.current === draft.id
          && selectedVpnPanelIdRef.current === draft.panelId
        if (submittedDraftIsCurrent) cancelInboundEdit()
      } else {
        cancelVpnPanelEdit()
        cancelInboundEdit()
      }
      await loadVpnPanelDetails(selectedVpnPanelIdRef.current, token, action.operationId)
    }
    throw new ApiClientError('VPN-объект уже изменен другим администратором. Данные обновлены: повторите действие с актуальной версией.', 409, error.payload)
  }

  const handleSaveVpnPanel = async () => {
    if (!token || !canWriteActiveSection) return
    const editingId = editingVpnPanelId
    const validationErrors = validateVpnPanelForm(vpnPanelForm, Boolean(editingId))
    if (validationErrors.length > 0) {
      setError(validationErrors[0])
      return
    }
    const submittedForm = vpnPanelForm
    const selectedPanelId = selectedVpnPanelIdRef.current
    const currentPanel = editingId ? vpnPanels.find((panel) => panel.id === editingId) : undefined
    if (currentPanel && !isVpnPanelFormChanged(submittedForm, currentPanel)) return
    await runAction('panels', 'vpn-panel-save', async (action) => {
      const saved = editingId
        ? await api.updateAdminVpnPanel(token, editingId, submittedForm)
            .catch((error: unknown) => throwVpnPanelConflict(error, action, { kind: 'panel', id: editingId, panelId: selectedPanelId, form: submittedForm }))
        : await api.createAdminVpnPanel(token, submittedForm)
      if (!action.isCurrent()) return
      setNotice(`VPN-панель ${saved.name} ${editingId ? 'обновлена' : 'сохранена'}. Пароль не возвращается из API.`)
      const formIsCurrent = vpnPanelFormRef.current === submittedForm
        && editingVpnPanelIdRef.current === editingId
        && selectedVpnPanelIdRef.current === selectedPanelId
      if (formIsCurrent) {
        selectVpnPanel(saved.id)
        setEditingVpnPanelId(null)
        setVpnPanelForm(defaultVpnPanelForm)
      }
      await action.reloadAll()
      if (formIsCurrent && action.isCurrent() && selectedVpnPanelIdRef.current === saved.id) {
        await loadVpnPanelDetails(saved.id, token, action.operationId)
      }
    }, vpnPanelActionResourceKey(editingId || 'create'))
  }

  const handleTestVpnPanel = (panelId: string) => runAction('panels', `test-${panelId}`, async (action) => {
    const result = await api.testAdminVpnPanel(token, panelId)
    if (!action.isCurrent()) return
    setNotice(`Проверка панели: ${formatStatusLabel(result.status)} (${result.version || 'версия неизвестна'})`)
    await action.reloadAll()
    await loadVpnPanelDetails(panelId, token, action.operationId)
  }, vpnPanelActionResourceKey(panelId))

  const handleSyncVpnPanel = (panelId: string) => runAction('panels', `sync-${panelId}`, async (action) => {
    const result = await api.syncAdminVpnPanel(token, panelId)
    if (!action.isCurrent()) return
    setNotice(`Синхронизация ${formatStatusLabel(result.status)}: ${result.summaryJson || result.errorMessage}`)
    await action.reloadAll()
    await loadVpnPanelDetails(panelId, token, action.operationId)
  }, vpnPanelActionResourceKey(panelId))

  const handleSetVpnPanelStatus = (panel: VpnPanelDto, status: 'Active' | 'Disabled') => runAction('panels', `panel-status-${panel.id}`, async (action) => {
    const saved = await api.updateAdminVpnPanel(token, panel.id, { status, revision: panel.revision })
      .catch((error: unknown) => throwVpnPanelConflict(error, action))
    if (!action.isCurrent()) return
    setNotice(`Панель ${saved.name}: статус ${formatStatusLabel(saved.status)}.`)
    await action.reloadAll()
    await loadVpnPanelDetails(panel.id, token, action.operationId)
  }, vpnPanelActionResourceKey(panel.id))

  const handleDeleteVpnPanel = (panel: VpnPanelDto) => runAction('panels', `panel-delete-${panel.id}`, async (action) => {
    const result = await api.deleteAdminVpnPanel(token, panel.id, panel.revision)
      .catch((error: unknown) => throwVpnPanelConflict(error, action))
    if (!action.isCurrent()) return
    setNotice(result.archived
      ? `Панель ${panel.name} отключена и сохранена в истории: связей ${result.linkedInbounds + result.linkedClients + result.linkedSyncRuns + result.linkedHealthChecks}.`
      : `Панель ${panel.name} удалена.`)
    if (selectedVpnPanelIdRef.current === panel.id && result.deleted) selectVpnPanel('')
    if (editingVpnPanelIdRef.current === panel.id) cancelVpnPanelEdit()
    await action.reloadAll()
  }, vpnPanelActionResourceKey(panel.id))

  const handleSaveInbound = () => {
    if (!token || !canWriteSection('panels')) return
    const editingId = editingInboundId
    const submittedForm = inboundForm
    const panelId = selectedVpnPanelId
    const validationErrors = validateInboundForm(submittedForm, panelId)
    if (validationErrors.length > 0) {
      setError(`Inbound: ${validationErrors.join(' ')}`)
      return
    }
    const currentInbound = editingId ? vpnInbounds.find((inbound) => inbound.id === editingId) : undefined
    if (currentInbound && !isVpnInboundFormChanged(submittedForm, currentInbound)) return
    const resourceKeys = editingId
      ? [vpnPanelActionResourceKey(panelId), vpnInboundActionResourceKey(editingId)]
      : vpnPanelActionResourceKey(panelId)
    return runAction('panels', editingId ? `update-inbound-${editingId}` : 'create-inbound', async (action) => {
      const saved = editingId
        ? await api.updateAdminVpnInbound(token, editingId, submittedForm)
            .catch((error: unknown) => throwVpnPanelConflict(error, action, { kind: 'inbound', id: editingId, panelId, form: submittedForm }))
        : await api.createAdminVpnPanelInbound(token, panelId, submittedForm)
      if (!action.isCurrent()) return
      setNotice(editingId ? `Inbound-правило ${saved.name} обновлено.` : `Inbound-правило ${saved.name} создано.`)
      if (inboundFormRef.current === submittedForm
        && editingInboundIdRef.current === editingId
        && selectedVpnPanelIdRef.current === panelId) {
        setEditingInboundId(null)
        setInboundForm(defaultInboundForm)
      }
      await loadVpnPanelDetails(panelId, token, action.operationId)
    }, resourceKeys)
  }

  const handleSetDefaultInbound = (inboundId: string) => runAction('panels', inboundId, async (action) => {
    const inbound = vpnInbounds.find((item) => item.id === inboundId)
    if (!inbound) throw new ApiClientError('Inbound больше не найден. Обновите список.', 409, null)
    await api.setAdminVpnInboundDefault(token, inboundId, inbound.revision)
      .catch((error: unknown) => throwVpnPanelConflict(error, action))
    if (!action.isCurrent()) return
    setNotice('Основное inbound-правило обновлено.')
    await loadVpnPanelDetails(selectedVpnPanelId, token, action.operationId)
  }, [vpnPanelActionResourceKey(vpnInbounds.find((inbound) => inbound.id === inboundId)?.vpnPanelId ?? selectedVpnPanelId), vpnInboundActionResourceKey(inboundId)])

  const handleToggleInboundActive = (inbound: VpnInboundDto) => runAction('panels', `toggle-inbound-${inbound.id}`, async (action) => {
    const nextIsActive = !inbound.isActive
    const saved = await api.updateAdminVpnInbound(token, inbound.id, inboundToForm(inbound, {
      isActive: nextIsActive,
      isDefault: nextIsActive ? inbound.isDefault : false
    })).catch((error: unknown) => throwVpnPanelConflict(error, action))
    if (!action.isCurrent()) return
    setNotice(nextIsActive ? `Inbound-правило ${saved.name} включено.` : `Inbound-правило ${saved.name} выключено.`)
    if (editingInboundIdRef.current === inbound.id) {
      setEditingInboundId(null)
      setInboundForm(defaultInboundForm)
    }
    await loadVpnPanelDetails(selectedVpnPanelId, token, action.operationId)
  }, [vpnPanelActionResourceKey(inbound.vpnPanelId), vpnInboundActionResourceKey(inbound.id)])

  const editInbound = (inbound: VpnInboundDto) => {
    setEditingInboundId(inbound.id)
    setInboundForm(inboundToForm(inbound))
  }

  const cancelInboundEdit = () => {
    setEditingInboundId(null)
    setInboundForm(defaultInboundForm)
  }

  const handleVpnClientAction = (client: VpnClientDto, action: 'enable' | 'disable' | 'sync' | 'reset') => runAction('panels', `vpn-client-${action}-${client.id}`, async (adminAction) => {
    try {
      const saved = await (action === 'enable'
        ? api.enableAdminVpnClient(token, client.id, client.revision)
        : action === 'disable'
          ? api.disableAdminVpnClient(token, client.id, client.revision)
          : action === 'sync'
            ? api.syncAdminVpnClient(token, client.id, client.revision)
            : api.resetAdminVpnClientTraffic(token, client.id, client.revision))
        .catch((error: unknown) => throwVpnPanelConflict(error, adminAction))
      if (!adminAction.isCurrent()) return
      setNotice(`VPN-клиент ${saved.email} обновлен: ${formatAdminDisplayLabel(saved.syncStatus)}.`)
    } finally {
      await loadVpnPanelDetails(selectedVpnPanelId, token, adminAction.operationId)
    }
  }, [vpnPanelActionResourceKey(client.vpnPanelId), vpnInboundActionResourceKey(client.vpnInboundId), vpnClientActionResourceKey(client.id)])

  const handleMigrateVpnClient = (client: VpnClientDto) => {
    const targetInboundId = vpnClientMigrationTargets[client.id]
    if (!targetInboundId) return
    const targetInbound = vpnMigrationInbounds.find((inbound) => inbound.id === targetInboundId)
    const targetPanel = vpnPanels.find((panel) => panel.id === targetInbound?.vpnPanelId)
    const resourceKeys = [
      vpnPanelActionResourceKey(client.vpnPanelId),
      vpnInboundActionResourceKey(client.vpnInboundId),
      vpnClientActionResourceKey(client.id),
      ...(targetInbound ? [vpnPanelActionResourceKey(targetInbound.vpnPanelId), vpnInboundActionResourceKey(targetInbound.id)] : [])
    ]
    return runAction('panels', `vpn-client-migrate-${client.id}`, async (action) => {
      const saved = await api.migrateAdminVpnClient(token, client.id, targetInboundId, client.revision)
        .catch((error: unknown) => throwVpnPanelConflict(error, action))
      if (!action.isCurrent()) return
      if (client.vpnPanelId !== saved.vpnPanelId) {
        setVpnPanels((current) => current.map((panel) => {
          if (panel.id === client.vpnPanelId) return { ...panel, usedCapacity: Math.max(0, panel.usedCapacity - 1) }
          if (panel.id === saved.vpnPanelId) return { ...panel, usedCapacity: panel.usedCapacity + 1 }
          return panel
        }))
      }
      setNotice(`VPN-клиент ${saved.email} перенесен: ${targetPanel?.name ?? shortId(saved.vpnPanelId)} · ${targetInbound?.name ?? shortId(saved.vpnInboundId)}.`)
      if (saved.vpnPanelId !== selectedVpnPanelIdRef.current) {
        selectVpnPanel(saved.vpnPanelId)
      } else {
        await loadVpnPanelDetails(saved.vpnPanelId, token, action.operationId)
      }
    }, resourceKeys)
  }

  const updateVpnClientMigrationTarget = (clientId: string, targetInboundId: string) => setVpnClientMigrationTargets((current) => ({ ...current, [clientId]: targetInboundId }))
  const migrationOptionGroupsForClient = (client: VpnClientDto) => {
    const currentInbound = vpnMigrationInbounds.find((inbound) => inbound.id === client.vpnInboundId)
      ?? vpnInbounds.find((inbound) => inbound.id === client.vpnInboundId)
    return vpnPanels
      .filter((panel) => panel.status === 'Active' && panel.healthStatus !== 'Unhealthy' && panel.usedCapacity < panel.capacity)
      .map((panel) => ({
        panel,
        inbounds: vpnMigrationInbounds
          .filter((inbound) => inbound.vpnPanelId === panel.id && inbound.id !== client.vpnInboundId && inbound.isActive && inbound.usedCapacity < inbound.capacity && (!currentInbound || inbound.protocol.toLowerCase() === currentInbound.protocol.toLowerCase()))
          .sort((left, right) => Number(right.isDefault) - Number(left.isDefault) || left.name.localeCompare(right.name))
      }))
      .filter((group) => group.inbounds.length > 0)
  }

  const editServer = (server: VpnNodeDto) => {
    setEditingServerId(server.id)
    setEditingServerRevision(server.revision)
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
    setEditingServerRevision(null)
    setServerForm(defaultServerForm)
  }

  const throwServerConflict = async (error: unknown, action: AdminActionContext, serverId: string): Promise<never> => {
    if (!(error instanceof ApiClientError) || error.status !== 409) throw error
    await action.reloadAll()
    if (action.isCurrent() && editingServerIdRef.current === serverId) cancelServerEdit()
    throw new ApiClientError('Сервер уже изменен другим администратором. Список обновлен. Повторите.', 409, error.payload)
  }

  const throwProvisioningConflict = async (error: unknown, action: AdminActionContext): Promise<never> => {
    if (!isProvisioningStateConflict(error)) throw error
    await action.reloadAll()
    throw new ApiClientError('Запуск уже изменён. Данные обновлены.', 409, null)
  }

  const handleSaveServer = async () => {
    if (!token || !canWriteActiveSection) return
    const editingId = editingServerId
    const submittedRevision = editingServerRevision
    const submittedForm = serverForm
    const validationErrors = validateServerForm(submittedForm)
    if (validationErrors.length > 0) {
      setError(`Сервер: ${validationErrors.join(' ')}`)
      return
    }
    await runAction('nodes', 'server-save', async (action) => {
      if (editingId && submittedRevision === null) throw new ApiClientError('Не удалось определить ревизию сервера. Откройте его заново.', 409, null)
      const saved = editingId
        ? await api.updateAdminServer(token, editingId, submittedForm, submittedRevision as number)
            .catch((error: unknown) => throwServerConflict(error, action, editingId))
        : await api.createAdminServer(token, submittedForm)
      if (!action.isCurrent()) return
      setNotice(`Сервер ${saved.name} ${editingId ? 'обновлен' : 'создан'}. Секреты не возвращаются из API.`)
      if (serverFormRef.current === submittedForm && editingServerIdRef.current === editingId) {
        setEditingServerId(null)
        setEditingServerRevision(null)
        setServerForm(defaultServerForm)
      }
      await action.reloadAll()
    }, serverActionResourceKey(editingId || 'create'))
  }

  const handleServerMode = async (server: VpnNodeDto, action: 'maintenance' | 'ready' | 'drain' | 'allocate' | 'disable') => {
    const actionLabel = action === 'maintenance' ? 'перевести в обслуживание' : action === 'ready' ? 'вернуть в работу' : action === 'drain' ? 'закрыть набор пользователей' : action === 'disable' ? 'отключить сервер' : 'открыть набор пользователей'
    await runAction('nodes', `${action}-${server.id}`, async (adminAction) => {
      const command = action === 'maintenance'
        ? api.enableAdminServerMaintenance(token, server.id, server.revision)
        : action === 'ready'
          ? api.disableAdminServerMaintenance(token, server.id, server.revision)
          : action === 'drain'
            ? api.disableAdminServerAllocation(token, server.id, server.revision)
            : action === 'allocate'
              ? api.enableAdminServerAllocation(token, server.id, server.revision)
              : api.disableAdminServer(token, server.id, server.revision)
      await command.catch((error: unknown) => throwServerConflict(error, adminAction, server.id))
      if (!adminAction.isCurrent()) return
      setNotice(`Сервер ${server.name}: ${actionLabel}.`)
      await adminAction.reloadAll()
    }, serverActionResourceKey(server.id))
  }

  const handleDeleteServer = (server: VpnNodeDto) => runAction('nodes', `delete-server-${server.id}`, async (action) => {
    const result = await api.deleteAdminServer(token, server.id, server.revision)
      .catch((error: unknown) => throwServerConflict(error, action, server.id))
    if (!action.isCurrent()) return
    setNotice(result.archived
      ? `Сервер ${server.name} архивирован: связей ${result.linkedSubscriptions + result.linkedAccesses + result.linkedProvisioningRuns + result.linkedHealthChecks + result.linkedMigrationJobs}.`
      : `Сервер ${server.name} удалён.`)
    if (editingServerIdRef.current === server.id) cancelServerEdit()
    await action.reloadAll()
  }, serverActionResourceKey(server.id))

  const handleCheckServerHealth = (server: VpnNodeDto) => runAction('nodes', `health-server-${server.id}`, async (action) => {
    const check = await api.checkAdminServerHealth(token, server.id)
    if (!action.isCurrent()) return
    setNotice(`Health-check ${server.name}: ${formatStatusLabel(check.status)}${check.errorText ? ` · ${check.errorText}` : ''}`)
    await action.reloadAll()
  }, serverActionResourceKey(server.id))

  const handleQueuePrecheck = (server: VpnNodeDto) => runAction('nodes', `precheck-${server.id}`, async (action) => {
    const response = await api.precheckAdminServer(token, server.id, server.revision)
      .catch((error: unknown) => throwProvisioningConflict(error, action))
    if (!action.isCurrent()) return
    setNotice(`Проверка поставлена в очередь. Режим: ${provisioningDeployModeLabel(response.mode)}. ID запуска: ${response.runId}`)
    await action.reloadAll()
  }, serverActionResourceKey(server.id))

  const handleQueueProvision = async (server: VpnNodeDto) => {
    await runAction('nodes', `provision-${server.id}`, async (action) => {
      const response = await api.queueAdminProvision(token, server.id, server.revision, false)
        .catch((error: unknown) => throwProvisioningConflict(error, action))
      if (!action.isCurrent()) return
      setNotice(`Подготовка сервера поставлена в очередь. Режим: ${provisioningDeployModeLabel(response.mode)}; риск: ${provisioningRiskLabel(response.riskLevel)}. ID запуска: ${response.runId}`)
      await action.reloadAll()
    }, serverActionResourceKey(server.id))
  }

  const provisioningActionResourceKeys = (runId: string) => {
    const run = provisioningRuns.find((item) => item.id === runId)
    return run
      ? [provisioningRunActionResourceKey(runId), serverActionResourceKey(run.nodeId)]
      : [provisioningRunActionResourceKey(runId)]
  }

  const handleRetryProvisioningRun = (run: ProvisioningRunDto) => runAction('provisioning', `retry-${run.id}`, async (action) => {
    const response = await api.retryAdminProvisioningRun(token, run.id, run.revision)
      .catch((error: unknown) => throwProvisioningConflict(error, action))
    if (!action.isCurrent()) return
    setNotice(`Повтор поставлен в очередь. Режим: ${provisioningDeployModeLabel(response.mode)}. Новый ID запуска: ${response.runId}`)
    await action.reloadAll()
  }, provisioningActionResourceKeys(run.id))

  const handleRetryNotificationDelivery = (deliveryId: string) => runAction('audit', `retry-notification-${deliveryId}`, async (action) => {
    await api.retryAdminNotificationDelivery(token, deliveryId)
    if (!action.isCurrent()) return
    setNotice('Email-уведомление возвращено в очередь доставки.')
    await action.reloadAll()
  }, notificationDeliveryActionResourceKey(deliveryId))

  const handleDeployProvisioningRun = (run: ProvisioningRunDto) => {
    return runAction('provisioning', `deploy-run-${run.id}`, async (action) => {
      const response = await api.deployAdminProvisioningRun(token, run.id, run.revision)
        .catch((error: unknown) => throwProvisioningConflict(error, action))
      if (!action.isCurrent()) return
      setNotice(`Развёртывание поставлено в очередь. Режим: ${provisioningDeployModeLabel(response.mode)}; риск: ${provisioningRiskLabel(response.riskLevel)}. ID запуска: ${response.runId}`)
      await action.reloadAll()
    }, provisioningActionResourceKeys(run.id))
  }

  const handleCancelProvisioningRun = (run: ProvisioningRunDto) => {
    return runAction('provisioning', `cancel-run-${run.id}`, async (action) => {
      await api.cancelAdminProvisioningRun(token, run.id, run.revision)
        .catch((error: unknown) => throwProvisioningConflict(error, action))
      if (!action.isCurrent()) return
      setNotice('Запуск подготовки сервера отменен.')
      await action.reloadAll()
    }, provisioningActionResourceKeys(run.id))
  }

  const handleProvisioningSupportNeeded = (run: ProvisioningRunDto) => runAction('provisioning', `support-run-${run.id}`, async (action) => {
    const response = await api.markAdminProvisioningSupportNeeded(token, run.id, run.revision)
      .catch((error: unknown) => throwProvisioningConflict(error, action))
    if (!action.isCurrent()) return
    setNotice(`Обращение в поддержку: ${response.supportConversationId}`)
    await action.reloadAll()
  }, provisioningActionResourceKeys(run.id))

  const handleSaveBotSettings = () => {
    if (!token || !canWriteSection('bot')) return
    const validationErrors = validateTelegramBotUrlFields(botSettingsForm)
    if (validationErrors.length > 0) {
      setError(validationErrors[0])
      return
    }
    if (!isTelegramBotSettingsFormChanged(botSettingsForm, botSettings)) {
      setError('Изменения настроек Telegram-бота не обнаружены.')
      return
    }
    return runAction('bot', 'bot-settings', async (action) => {
      const submittedForm = botSettingsForm
      const submittedRevision = botSettings.revision
      let saved: AdminTelegramBotSettingsDto
      try {
        saved = await api.updateAdminTelegramBotSettings(token, submittedForm, submittedRevision)
      } catch (actionError) {
        if (actionError instanceof ApiClientError && actionError.status === 409 && action.isCurrent()) {
          try {
            const latest = await api.getAdminTelegramBotSettings(token)
            if (action.isCurrent()) {
              setBotSettings(latest)
              if (botSettingsFormRef.current === submittedForm) {
                botSettingsFormDirty.current = false
                setBotSettingsForm(telegramBotSettingsToForm(latest))
              }
              botSettingsCheckRequestId.current += 1
              setBotSettingsCheck(null)
            }
          } catch {
            // Preserve the original conflict as the actionable error.
          }
        }
        throw actionError
      }
      if (!action.isCurrent()) return
      setBotSettings(saved)
      if (botSettingsFormRef.current === submittedForm) {
        botSettingsFormDirty.current = false
        setBotSettingsForm(telegramBotSettingsToForm(saved))
      }
      botSettingsCheckRequestId.current += 1
      setBotSettingsCheck(null)
      setNotice('Настройки Telegram-бота сохранены. Токены остаются скрытыми и не возвращаются из API.')
    }, botSettingsActionResourceKey)
  }

  const handleTestBotSettings = () => {
    if (!token || !canWriteSection('bot')) return
    return runAction('bot', 'bot-settings-test', async (action) => {
      const requestId = ++botSettingsCheckRequestId.current
      const result = await api.testAdminTelegramBotSettings(token)
      if (!action.isCurrent() || requestId !== botSettingsCheckRequestId.current) return
      setBotSettingsCheck(result)
      setNotice(result.isReady ? 'Telegram-бот готов к работе.' : 'Проверка Telegram-бота нашла настройки, которые нужно заполнить.')
    }, botSettingsActionResourceKey)
  }

  const providerFormSetup = providerSetup(providerForm.provider)
  const editingProviderAccount = editingProviderAccountId
    ? paymentProviderAccounts.find((account) => account.id === editingProviderAccountId)
    : undefined
  const providerFormErrors = validatePaymentProviderForm(providerForm, providerFormSetup, editingProviderAccount)
  const providerFormActionBusy = isActionResourceBusy(paymentProviderActionResourceKey(editingProviderAccountId || 'create'))
  const tariffFormErrors = validateTariffForm(tariffForm, tariffFeaturesText)
  const editingTariff = editingTariffId ? tariffs.find((tariff) => tariff.id === editingTariffId) : undefined
  const tariffFormChanged = !editingTariffId || Boolean(editingTariff && isTariffFormChanged(tariffForm, tariffFeaturesText, editingTariff))
  const tariffFormActionBusy = isActionResourceBusy(tariffActionResourceKey(editingTariffId || 'create'))
  const editingRelease = editingReleaseId ? appReleases.find((release) => release.id === editingReleaseId) : undefined
  const releaseFormChanged = !editingReleaseId || Boolean(editingRelease && isAppReleaseFormChanged(releaseForm, editingRelease))
  const releaseFormActionBusy = isActionResourceBusy(appReleaseActionResourceKey(editingReleaseId || 'create'))
  const editingFaq = editingFaqId ? faqEntries.find((entry) => entry.id === editingFaqId) : undefined
  const faqFormChanged = !editingFaqId || Boolean(editingFaq && isFaqFormChanged(faqForm, editingFaq))
  const faqFormActionBusy = isActionResourceBusy(faqActionResourceKey(editingFaqId || 'create'))
  const editingSiteContent = editingSiteContentId ? siteContentBlocks.find((block) => block.id === editingSiteContentId) : undefined
  const siteContentFormChanged = !editingSiteContentId || Boolean(editingSiteContent && isSiteContentFormChanged(siteContentForm, editingSiteContent))
  const siteContentActionBusy = isActionResourceBusy(siteContentActionResourceKey)
  const editingWorkScenario = editingWorkScenarioId ? workScenarios.find((scenario) => scenario.id === editingWorkScenarioId) : undefined
  const workScenarioFormChanged = !editingWorkScenarioId || Boolean(editingWorkScenario && isWorkScenarioFormChanged(workScenarioForm, editingWorkScenario))
  const workScenarioFormActionBusy = isActionResourceBusy(workScenarioActionResourceKey(editingWorkScenarioId || 'create'))
  const botSettingsActionBusy = isActionResourceBusy(botSettingsActionResourceKey)
  const editingReferralProgram = editingReferralProgramId ? referralPrograms.find((program) => program.id === editingReferralProgramId) : undefined
  const referralProgramFormChanged = !editingReferralProgramId || Boolean(editingReferralProgram && isReferralProgramFormChanged(referralProgramForm, editingReferralProgram))
  const referralProgramFormActionBusy = isActionResourceBusy(referralProgramActionResourceKey)
  const serverFormErrors = validateServerForm(serverForm)
  const serverFormActionBusy = isActionResourceBusy(serverActionResourceKey(editingServerId || 'create'))
  const editingVpnPanel = editingVpnPanelId ? vpnPanels.find((panel) => panel.id === editingVpnPanelId) : undefined
  const vpnPanelFormChanged = !editingVpnPanelId || Boolean(editingVpnPanel && isVpnPanelFormChanged(vpnPanelForm, editingVpnPanel))
  const vpnPanelFormActionBusy = isActionResourceBusy(vpnPanelActionResourceKey(editingVpnPanelId || 'create'))
  const editingInbound = editingInboundId ? vpnInbounds.find((inbound) => inbound.id === editingInboundId) : undefined
  const inboundFormChanged = !editingInboundId || Boolean(editingInbound && isVpnInboundFormChanged(inboundForm, editingInbound))
  const inboundFormActionBusy = Boolean(selectedVpnPanelId && isActionResourceBusy(
      vpnPanelActionResourceKey(selectedVpnPanelId),
      ...(editingInboundId ? [vpnInboundActionResourceKey(editingInboundId)] : [])
    ))
  const vpnPanelFormErrors = validateVpnPanelForm(vpnPanelForm, Boolean(editingVpnPanelId))
  const inboundFormErrors = validateInboundForm(inboundForm, selectedVpnPanelId)
  const workScenarioFormErrors = validateWorkScenarioForm(workScenarioForm)
  const botSettingsFormErrors = validateTelegramBotUrlFields(botSettingsForm)
  const botSettingsFormChanged = isTelegramBotSettingsFormChanged(botSettingsForm, botSettings)

  if (token && !adminAccessVerified) {
    return (
      <PageShell title="Админ-панель VPN Platform">
        <SkipLink href="#admin-session-recovery" updateHash={false} />
        <div id="admin-session-recovery" className="admin-login-shell" tabIndex={-1}>
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
                <p className="eyebrow">Защищённый доступ</p>
                <h2 className="page-heading">Восстановление admin-сессии</h2>
                <p className="muted no-margin-bottom">
                  {sessionHydrating
                    ? 'Проверяем сохранённую сессию и административные полномочия.'
                    : 'Не удалось подтвердить административный доступ. Сессия сохранена для повторной проверки.'}
                </p>
              </div>
              <ValidationModeBadge label="Доступ только для администраторов" />
            </div>
            <div className="toolbar mt-12">
              <PrimaryButton type="button" disabled={sessionHydrating || logoutBusy} aria-busy={sessionHydrating} onClick={() => void hydrateRestoredAdminSession(token, refreshToken)}>Повторить проверку</PrimaryButton>
              <PrimaryButton type="button" disabled={logoutBusy} aria-busy={logoutBusy} className="button-secondary" onClick={() => void handleLogout()}>Завершить сессию</PrimaryButton>
            </div>
            {busy && <LoadingBlock label="Проверяем административный доступ..." />}
            {error && <ErrorBlock message={error} />}
          </Card>
        </div>
      </PageShell>
    )
  }

  if (!token) {
    return (
      <PageShell title="Админ-панель VPN Platform">
        <SkipLink href="#admin-login" updateHash={false} />
        <div id="admin-login" className="admin-login-shell" tabIndex={-1}>
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
        </div>
      </PageShell>
    )
  }

  if (!adminDataReady) {
    return (
      <PageShell title="Админ-панель VPN Platform">
        <div id="admin-data-loading" className="admin-login-shell" tabIndex={-1}>
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
                <p className="eyebrow">Доступ подтверждён</p>
                <h2 className="page-heading">Загрузка admin-панели</h2>
                <p className="muted no-margin-bottom">Получаем фактические рабочие данные перед показом метрик, очередей и разделов управления.</p>
              </div>
              <ValidationModeBadge label="Доступ только для администраторов" />
            </div>
            {busy
              ? <LoadingBlock label="Загружаем данные admin-panel..." />
              : (
                <div className="toolbar mt-12">
                  <PrimaryButton type="button" onClick={() => void loadAll(token, adminSession)}>Повторить загрузку</PrimaryButton>
                  <PrimaryButton type="button" disabled={logoutBusy} aria-busy={logoutBusy} className="button-secondary" onClick={() => void handleLogout()}>Завершить сессию</PrimaryButton>
                </div>
              )}
            {error && <ErrorBlock message={error} />}
          </Card>
        </div>
      </PageShell>
    )
  }

  return (
    <PageShell title="Админ-панель VPN Platform">
      <SkipLink href="#admin-content" updateHash={false} />
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
                    aria-controls={activeSection === id && activeSectionLoadFailed ? 'admin-section-load-error' : id}
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
      {loadErrors.length > 0 && !activeSectionLoadFailed && <ErrorBlock message={`Не удалось загрузить часть данных (${loadErrors.length}). Откройте затронутый раздел и повторите загрузку.`} />}

      {activeSectionLoadFailed && (
        <div id="admin-section-load-error" className="section" role="tabpanel" aria-labelledby={adminSectionTabId(activeSection)}>
          <Card>
            <ErrorBlock message={`Не удалось загрузить раздел «${activeSectionLabel}».`} />
            <p className="muted">Данные раздела скрыты, чтобы ошибка API не выглядела как подтверждённый пустой результат.</p>
            <CodeBlock>{activeSectionLoadErrors.map((item) => `${item.area}: ${item.message}`).join('\n')}</CodeBlock>
            <div className="toolbar mt-12">
              <PrimaryButton type="button" disabled={busy} aria-busy={busy} onClick={() => void loadAll(token)}>Повторить загрузку раздела</PrimaryButton>
            </div>
          </Card>
        </div>
      )}

      <div id="dashboard" className="grid section" role="tabpanel" aria-labelledby={adminSectionTabId('dashboard')} hidden={activeSection !== 'dashboard' || activeSectionLoadFailed}>
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
        <div className="section" hidden={activeSection !== 'dashboard' || activeSectionLoadFailed}>
          <SectionCard
            title={canReadFinance ? 'Готовность к live-продажам' : 'Готовность инфраструктуры'}
            description={canReadFinance
              ? 'Проверка показывает, можно ли принимать production-платежи и автоматически выдавать реальный VPN-доступ через 3x-ui.'
              : 'Показаны только инфраструктурные проверки, доступные текущей административной роли.'}
            actions={<StatusBadge value={summary.productionReadiness.status} />}
          >
            <div className="list-stack">
              {summary.productionReadiness.checks.map((check) => {
                const actionSection = parseAdminSectionHref(check.actionHref)
                const canOpenAction = actionSection !== null && availableAdminSectionIds.has(actionSection)
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
                        href={`#${actionSection}`}
                        onClick={(event) => {
                          event.preventDefault()
                          goToAdminSection(actionSection)
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

      <div className="section card-list-two" hidden={activeSection !== 'dashboard' || activeSectionLoadFailed}>
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
            {dashboardOpenSupportConversations.map((conversation) => <div key={conversation.id} className="list-item"><span>{conversation.subject || 'Обращение в поддержку'} · tg:{conversation.telegramUserId ?? '—'}</span><StatusBadge value={conversation.status} /></div>)}
            {dashboardFailedPayments.length === 0 && dashboardFailedProvisioningRuns.length === 0 && dashboardOpenSupportConversations.length === 0 && <EmptyState title="Нет срочных проблем" description="В доступных очередях сейчас нет ошибок, требующих реакции." />}
          </div>
        </SectionCard>
      </div>

      <div id="audit" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('audit')} hidden={activeSection !== 'audit' || activeSectionLoadFailed}>
        <Card>
          <h3>Журнал аудита</h3>
          <form className="toolbar toolbar-form" aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void loadAll(token) }}>
            <label><span>Действие</span><input value={auditActionFilter} onChange={(e) => setAuditActionFilter(e.target.value)} placeholder="payment.status.changed" /></label>
            <label><span>Сущность</span><input value={auditEntityTypeFilter} onChange={(e) => setAuditEntityTypeFilter(e.target.value)} placeholder="PaymentAttempt" /></label>
            <label><span>Инициатор</span><select value={auditActorTypeFilter} onChange={(e) => setAuditActorTypeFilter(e.target.value)}><option value="">Все</option><option value="admin">Администратор</option><option value="system">Система</option><option value="user">Пользователь</option></select></label>
            <label><span>Поиск</span><input value={auditSearch} onChange={(e) => setAuditSearch(e.target.value)} placeholder="ID, инициатор или действие" /></label>
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
                    <div className="muted">Инициатор: {formatAdminDisplayLabel(entry.actorType)}/{entry.actorId || 'Неизвестно'} · IP: {entry.ip || '—'}</div>
                  </div>
                  <div className="item-status">
                    <StatusBadge value={formatAdminDisplayLabel(entry.actorType)} />
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
                      disabled={isActionResourceBusy(notificationDeliveryActionResourceKey(delivery.id))}
                      aria-busy={isActionResourceBusy(notificationDeliveryActionResourceKey(delivery.id))}
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
            {canReadFinance && <div className="list-item"><span>Изменения платежных провайдеров и ротация секретов</span><StatusBadge value={formatAdminDisplayLabel('finance')} /></div>}
            {canReadFinance && <div className="list-item"><span>Переходы статусов платежей из webhook и recheck</span><StatusBadge value={formatAdminDisplayLabel('system')} /></div>}
            {canReadSupport && <div className="list-item"><span>Ответы, заметки и статусы обращений</span><StatusBadge value={formatAdminDisplayLabel('support')} /></div>}
            {adminSession?.capabilities.botManage && <div className="list-item"><span>Настройки и ротация секретов Telegram-бота</span><StatusBadge value={formatAdminDisplayLabel('bot')} /></div>}
            <div className="list-item"><span>VPN provisioning и lifecycle-действия доступа</span><StatusBadge value={formatAdminDisplayLabel('vpn')} /></div>
          </div>
        </Card>
      </div>

      <div id="users" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('users')} hidden={activeSection !== 'users' || activeSectionLoadFailed}>
        <Card>
          <h3>Пользователи</h3>
          <form className="toolbar toolbar-form" aria-busy={busy || usersLoading} onSubmit={(event) => { event.preventDefault(); void loadUsers() }}>
            <label><span>Поиск</span><input value={userSearch} onChange={(e) => setUserSearch(e.target.value)} placeholder="email, имя или реферальный код" /></label>
            <label><span>Статус</span><select value={userStatusFilter} onChange={(e) => setUserStatusFilter(e.target.value)}><option value="">Все</option><option value="Active">Активные</option><option value="Suspended">Ограниченные</option><option value="Deleted">Удалённые</option><option value="New">Новые</option></select></label>
            <PrimaryButton type="submit" disabled={!token || busy || usersLoading} title={adminDisabledTitle} aria-busy={busy || usersLoading}>Применить</PrimaryButton>
          </form>
          {usersLoading && <LoadingBlock label="Загружаем пользователей..." />}
          {usersError && !usersLoading && (
            <div className="toast-error detail-load-error mt-12" role="alert">
              <p>Не удалось загрузить пользователей: {usersError}</p>
              <PrimaryButton type="button" className="button-secondary" disabled={busy || usersLoading} aria-busy={usersLoading} onClick={() => void loadUsers()}>
                Повторить загрузку пользователей
              </PrimaryButton>
            </div>
          )}
          {!usersLoading && !usersError && (
            <div className="list-stack mt-12">
              {users.length === 0 && <EmptyState title="Пользователи не найдены" description="Попробуйте изменить поиск или статус." />}
              {users.slice(0, 20).map((user) => (
                <div key={user.id} className={`list-item${selectedUserId === user.id ? ' selected-item' : ''}`}>
                  <div>
                    <strong>{s(user.displayName, 'Без имени')}</strong>
                    <div className="muted">{s(user.email)} · {formatAdminDisplayLabel(s(user.authSource))} · {s(user.referralCode)}</div>
                    <div className="muted">создан {formatDate(user.createdAt)} · вход {formatDate(user.lastLoginAt)}</div>
                  </div>
                  <div className="actions">
                    <StatusBadge value={user.isBlocked ? 'Blocked' : user.status} />
                    <StatusBadge value={formatAdminRoleLabels(user.rolesCsv)} />
                    <PrimaryButton className={selectedUserId === user.id ? 'button-secondary' : 'button-ghost'} onClick={() => selectAdminUser(user.id)}>{selectedUserId === user.id ? 'Открыто' : 'Открыть'}</PrimaryButton>
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
        <Card className="user-overview-card">
          <h3>Карточка пользователя</h3>
          {userOverviewLoading && <LoadingBlock label="Загружаем карточку пользователя..." />}
          {userOverviewError && selectedUserId && (
            <div className="toast-error detail-load-error" role="alert">
              <p>Не удалось загрузить карточку пользователя: {userOverviewError}</p>
              <PrimaryButton type="button" className="button-secondary" disabled={userOverviewLoading} aria-busy={userOverviewLoading} onClick={() => void loadUserOverview(selectedUserId, token, sessionOperationId.current)}>
                Повторить загрузку карточки
              </PrimaryButton>
            </div>
          )}
          {!userOverviewLoading && !userOverviewError && !userOverview && <p className="muted">Выберите пользователя.</p>}
          {userOverview && <>
            <div className="user-profile-head">
              <div>
                <strong>{s(userOverview.user.displayName, 'Без имени')}</strong>
                <div className="muted">{s(userOverview.user.email)} · {formatAdminDisplayLabel(s(userOverview.user.authSource))} · язык {s(userOverview.user.preferredLanguage)}</div>
                <div className="muted">ID {shortId(userOverview.user.id)} · реферал {s(userOverview.user.referralCode)} · создан {formatDate(userOverview.user.createdAt)}</div>
              </div>
              <div className="row-actions">
                <StatusBadge value={userOverview.user.isBlocked ? 'Blocked' : userOverview.user.status} />
                <StatusBadge value={formatAdminDisplayLabel(userOverview.user.emailConfirmed ? 'EmailConfirmed' : 'EmailNotConfirmed')} />
              </div>
            </div>
            {canWriteSection('users') && (
              <div className="user-profile-editor mt-12" aria-busy={isActionResourceBusy(userActionResourceKey(userOverview.user.id))}>
                <fieldset className="form-section">
                  <legend>Управление профилем</legend>
                  <div className="form-grid">
                    <label>
                      <span>Имя</span>
                      <input value={userEditForm.displayName} maxLength={80} onChange={(event) => setUserEditForm((current) => ({ ...current, displayName: event.target.value }))} />
                    </label>
                    <label>
                      <span>Статус аккаунта</span>
                      <select value={userEditForm.status} onChange={(event) => setUserEditForm((current) => ({ ...current, status: event.target.value }))}>
                        <option value="New">Новый</option>
                        <option value="Active">Активный</option>
                        <option value="Suspended">Ограниченный</option>
                        <option value="Deleted">Удалённый</option>
                      </select>
                    </label>
                  </div>
                  <label className="checkbox-row"><input type="checkbox" checked={userEditForm.isBlocked} onChange={(event) => setUserEditForm((current) => ({ ...current, isBlocked: event.target.checked }))} /> Заблокирован вручную</label>
                  <p className="muted">Ограничение, удаление или ручная блокировка активного пользователя завершает все его текущие сессии.</p>
                </fieldset>
                <FormValidationSummary errors={userEditChanged ? userEditErrors : []} />
                <div className="form-footer">
                  {userEditRevokesSessions
                    ? <ConfirmButton className="button-danger" disabled={!userEditChanged || userEditErrors.length > 0 || isActionResourceBusy(userActionResourceKey(userOverview.user.id))} message={`Ограничить доступ пользователя "${userOverview.user.displayName}" и завершить все его активные сессии?`} onConfirm={handleSaveAdminUser}>Сохранить и завершить сессии</ConfirmButton>
                    : <PrimaryButton type="button" disabled={!userEditChanged || userEditErrors.length > 0 || isActionResourceBusy(userActionResourceKey(userOverview.user.id))} aria-busy={isActionResourceBusy(userActionResourceKey(userOverview.user.id))} onClick={() => void handleSaveAdminUser()}>Сохранить профиль</PrimaryButton>}
                  <PrimaryButton type="button" className="button-ghost" disabled={!userEditChanged || isActionResourceBusy(userActionResourceKey(userOverview.user.id))} onClick={() => setUserEditForm(adminUserToEditForm(userOverview.user))}>Отменить изменения</PrimaryButton>
                </div>
              </div>
            )}
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
              <div className="card-head"><h4>Подписки</h4><StatusBadge value={`Активных: ${userOverviewStats.activeSubscriptionsCount}`} /></div>
              <div className="list-stack">
                {userOverview.subscriptions.length === 0 && <EmptyState title="Подписок нет" description="После оплаты тарифов подписки появятся здесь." />}
                {userOverview.subscriptions.slice(0, 5).map((subscription) => (
                  <div key={subscription.id} className="list-item">
                    <div>
                      <strong>{subscription.tariffName || shortId(subscription.tariffId)}</strong>
                      <div className="muted">{formatDate(subscription.startAt)} - {formatDate(subscription.endAt)} · {subscription.sourceChannel ? formatStatusLabel(subscription.sourceChannel) : '—'} · продлений {subscription.renewalCount ?? 0}</div>
                      <div className="muted">сервер {shortId(subscription.currentServerId)} · доступ {shortId(subscription.currentAccessId)}</div>
                    </div>
                    <StatusBadge value={subscription.status} />
                  </div>
                ))}
              </div>
            </div>

            <div className="user-overview-section">
              <div className="card-head"><h4>Заказы и платежи</h4><StatusBadge value={`Платежей: ${userOverviewStats.paymentsCount}`} /></div>
              <div className="list-stack">
                {userOverview.orders.length === 0 && userOverview.payments.length === 0 && <EmptyState title="Покупок нет" description="Заказы и платежи появятся после первого checkout." />}
                {userOverview.orders.slice(0, 4).map((order) => (
                  <div key={order.id} className="list-item">
                    <div>
                      <strong>{order.tariffName || shortId(order.tariffId)} · {order.amount} {order.currency}</strong>
                      <div className="muted">{order.channel ? formatStatusLabel(order.channel) : '—'} · {order.paymentProvider || '—'} · создан {formatDate(order.createdAt)} · оплачен {formatDate(order.paidAt)}</div>
                    </div>
                    <StatusBadge value={order.status} />
                  </div>
                ))}
                {userOverview.payments.slice(0, 4).map((payment) => (
                  <div key={payment.id} className="list-item">
                    <div>
                      <strong>{payment.provider} · {payment.amount} {payment.currency}</strong>
                      <div className="muted">Платёж {payment.providerPaymentId || shortId(payment.id)} · подпись {payment.signatureValidated ? 'проверена' : 'не проверена'} · активация {payment.isActivationProcessed ? 'выполнена' : 'ожидает'}</div>
                    </div>
                    <StatusBadge value={payment.status} />
                  </div>
                ))}
              </div>
            </div>

            <div className="user-overview-section">
              <div className="card-head"><h4>VPN-доступы</h4><StatusBadge value={`Активных: ${userOverviewStats.activeAccessesCount}`} /></div>
              <div className="list-stack">
                {userOverview.accessCredentials.length === 0 && <EmptyState title="VPN-доступов нет" description="Доступы создаются после успешной оплаты и сценария выдачи." />}
                {userOverview.accessCredentials.slice(0, 5).map((access) => (
                  <div key={access.id} className="list-item">
                    <div>
                      <strong>{formatStatusLabel(access.providerType)} · {access.serverName || shortId(access.serverId)}</strong>
                      <div className="muted">выдан {formatDate(access.issuedAt)} · синхронизация {formatDate(access.lastSyncedAt)} · ревизия {access.revision}</div>
                      {getAdminAccessTerminalReason(access, adminAccessNow)
                        ? <div className="muted user-overview-link">Ключ скрыт: подписка или доступ завершены.</div>
                        : <div className="muted user-overview-link">{access.accessUri || 'URI не выдан'}</div>}
                    </div>
                    <div className="item-status"><StatusBadge value={access.status} />{access.subscriptionStatus && <StatusBadge value={access.subscriptionStatus} />}</div>
                  </div>
                ))}
              </div>
            </div>

            <div className="user-overview-section">
              <div className="card-head"><h4>Telegram и поддержка</h4><StatusBadge value={`Аккаунтов: ${userOverviewStats.telegramAccountsCount}`} /></div>
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
                      <div className="muted">{formatStatusLabel(conversation.channel)} · tg:{conversation.telegramUserId ?? '—'} · обновлено {formatDate(conversation.updatedAt)}</div>
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

      <div id="payments" className="section" role="tabpanel" aria-labelledby={adminSectionTabId('payments')} hidden={activeSection !== 'payments' || activeSectionLoadFailed}>
        <div className="card-list-two">
        <Card>
          <h3>{editingProviderAccountId ? 'Редактирование способа оплаты' : 'Способы оплаты'}</h3>
          <p className="muted">Добавьте платежный аккаунт, включите его и проверьте готовность к оплатам. Секреты сохраняются скрыто.</p>
          <form hidden={!canWriteSection('payments')} aria-busy={providerFormActionBusy} onSubmit={(event) => { event.preventDefault(); void handleSaveProviderAccount() }}>
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
            <FormValidationSummary errors={providerForm !== defaultProviderForm ? providerFormErrors : []} />
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={providerFormActionBusy || !token || providerFormErrors.length > 0} title={adminDisabledTitle} aria-busy={providerFormActionBusy}>{editingProviderAccountId ? 'Сохранить изменения' : 'Сохранить способ оплаты'}</PrimaryButton>
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
                    <div className="muted">{providerSetup(account.provider).title} · {formatStatusLabel(account.mode)} · {providerSetup(account.provider).channel === 'web' ? 'показывается на сайте после готовности' : 'только Telegram-бот'} · {account.name}</div>
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
                  <PrimaryButton className="button-secondary" disabled={isActionResourceBusy(paymentProviderActionResourceKey(account.id))} onClick={() => editProviderAccount(account)}>Редактировать</PrimaryButton>
                  <PrimaryButton className="button-secondary" disabled={isActionResourceBusy(paymentProviderActionResourceKey(account.id))} onClick={() => void handleCheckProviderAccount(account)}>Проверить настройки</PrimaryButton>
                  {account.isEnabled ? <ConfirmButton className="button-danger" disabled={isActionResourceBusy(paymentProviderActionResourceKey(account.id))} message={`Отключить способ оплаты "${account.publicName}"? Пользователи больше не увидят его при оплате.`} onConfirm={() => handleSetProviderEnabled(account, false)}>Выключить</ConfirmButton> : <PrimaryButton className="button-ghost" disabled={isActionResourceBusy(paymentProviderActionResourceKey(account.id))} onClick={() => void handleSetProviderEnabled(account, true)}>Включить</PrimaryButton>}
                </div>
              </div>
            ))}
          </div>
        </Card>
        </div>

        <div className="section card-list-two">
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
                    <div className="muted">Пользователь: {order.userDisplayName || order.userEmail || shortId(order.userId)} · канал: {order.channel ? formatStatusLabel(order.channel) : '—'} · тип: {order.type ? formatStatusLabel(order.type) : '—'}</div>
                    <div className="muted">Создан: {formatDate(order.createdAt)} · истекает: {formatDate(order.expiresAt)} · оплачен: {formatDate(order.paidAt)}</div>
                    <div className="muted">Провайдер: {order.paymentProvider ?? '—'} · попыток оплаты: {order.paymentAttemptsCount ?? 0} · последний платеж: {shortId(order.lastPaymentId)} {order.lastPaymentStatus ? `(${formatStatusLabel(order.lastPaymentStatus)})` : ''}</div>
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
                  <PrimaryButton hidden={!canWriteSection('payments')} disabled={Boolean(getAdminOrderPaymentRecheckBlocker(order)) || isActionResourceBusy(orderActionResourceKey(order.id), ...(order.lastPaymentId ? [paymentActionResourceKey(order.lastPaymentId)] : []))} title={getAdminOrderPaymentRecheckBlocker(order) ?? undefined} onClick={() => void handleRecheckOrderPayment(order)}>Проверить оплату</PrimaryButton>
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
              const paymentActionBusy = isActionResourceBusy(...paymentActionResourceKeys(payment.id, payment.orderId))
              return (
                <div id={`payment-${payment.id}`} key={payment.id} className="list-item-vertical">
                  <div className="item-head">
                    <div>
                      <strong>{payment.provider} · {payment.amount} {payment.currency}</strong>
                      <div className="muted">Заказ: {shortId(payment.orderId)} · транзакция: {payment.providerPaymentId || '—'} · режим {payment.providerMode ? formatStatusLabel(payment.providerMode) : '—'}</div>
                      <div className="muted">Активация: {payment.isActivationProcessed ? 'обработана' : 'ожидает'} · возвращено {payment.refundedAmount ?? 0} {payment.currency} · доступно к возврату {refundableAmount} {payment.currency}</div>
                      {refundBlocker && <div className="safe-note">Возврат недоступен: {refundBlocker}</div>}
                    </div>
                    <div className="item-status">
                      <StatusBadge value={payment.status} />
                      <StatusBadge value={refundAllowed ? 'Refund ready' : 'Refund blocked'} />
                    </div>
                  </div>
                  <div className="toolbar" hidden={!canWriteSection('payments')}>
                    <PrimaryButton disabled={paymentActionBusy || Boolean(getAdminPaymentRecheckBlocker(payment))} title={getAdminPaymentRecheckBlocker(payment) ?? undefined} onClick={() => void handleRecheckPayment(payment.id)}>Проверить статус</PrimaryButton>
                    <label className="inline-number-field">
                      <span>Сумма</span>
                      <input value={refundAmount} onChange={(e) => setRefundAmounts((current) => ({ ...current, [payment.id]: Number(e.target.value) || 0 }))} type="number" min={0} max={refundableAmount} step="0.01" inputMode="decimal" disabled={!refundAllowed || paymentActionBusy} />
                    </label>
                    <label className="compact-field">
                      <span>Причина</span>
                      <input value={refundReason} onChange={(e) => setRefundReasons((current) => ({ ...current, [payment.id]: e.target.value }))} placeholder="manual_admin_refund" maxLength={120} disabled={!refundAllowed || paymentActionBusy} />
                    </label>
                    <ConfirmButton disabled={paymentActionBusy || !refundAllowed || refundAmount <= 0 || refundAmount > refundableAmount} className="button-secondary" message={`Вернуть ${refundAmount} ${payment.currency} по платежу ${shortId(payment.id)}? Действие будет записано в аудит.`} onConfirm={() => handleRefundPayment(payment)}>Вернуть платеж</ConfirmButton>
                  </div>
                </div>
              )
            })}
            {paymentWebhookEvents.length > 0 && <h4>Последние вебхуки</h4>}
            {paymentWebhookEvents.slice(0, 4).map((event) => (
              <div key={event.id} className="list-item-vertical">
                <div className="item-head">
                  <div>
                    <strong>{event.provider} · {event.eventType}</strong>
                    <div className="muted">Получен: {formatDate(event.receivedAt)} · подпись {event.signatureValidated ? 'проверена' : 'не проверена'}</div>
                    <div className="muted">Платёж: {shortId(event.paymentAttemptId)} · событие: {shortId(event.externalEventId)}</div>
                  </div>
                  <div className="item-status">
                    <StatusBadge value={event.status} />
                    {event.requiresAttention && <StatusBadge value="Требует внимания" />}
                  </div>
                </div>
                {event.isRetryable && <div className="safe-note">Событие допускает повторную доставку.</div>}
              </div>
            ))}
            {refunds.slice(0, 4).map((refund) => {
              const blocker = refundRecheckBlockerText(refund)
              const retryBlocker = refundRetryBlockerText(refund)
              const refundBusy = isActionResourceBusy(...refundActionResourceKeys(refund))
              return (
                <div key={refund.id} className="list-item-vertical">
                  <div className="item-head">
                    <span>Возврат {refund.amount} {refund.currency} · {refund.providerRefundId || shortId(refund.id)}</span>
                    <StatusBadge value={refund.status} />
                  </div>
                  <div className="toolbar" hidden={!canWriteSection('payments')}>
                    <PrimaryButton disabled={refundBusy || Boolean(blocker)} title={blocker || undefined} onClick={() => void handleRecheckRefund(refund)}>Сверить возврат</PrimaryButton>
                    <ConfirmButton disabled={refundBusy || Boolean(retryBlocker)} className="button-secondary" message={`Повторить возврат ${refund.amount} ${refund.currency}? Будут использованы сохранённые параметры и тот же idempotency key.`} onConfirm={() => handleRetryRefund(refund)}>Повторить возврат</ConfirmButton>
                  </div>
                </div>
              )
            })}
          </div>
        </Card>
        </div>
      </div>

      <div id="tariffs" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('tariffs')} hidden={activeSection !== 'tariffs' || activeSectionLoadFailed}>
        <Card>
          <h3>{editingTariffId ? 'Редактирование тарифа' : 'Новый тариф'}</h3>
          <form hidden={!canWriteSection('tariffs')} aria-busy={tariffFormActionBusy} onSubmit={(event) => { event.preventDefault(); void handleSaveTariff() }}>
            <fieldset className="form-section">
              <legend>Цена и срок</legend>
              <div className="form-grid">
                <label><span>Название</span><input value={tariffForm.name ?? ''} onChange={(e) => updateTariffForm('name', e.target.value)} placeholder="Например, Месяц VPN" maxLength={200} required /></label>
                <label><span>Slug</span><input value={tariffForm.slug ?? ''} onChange={(e) => updateTariffForm('slug', e.target.value)} placeholder="month-vpn" maxLength={160} /></label>
                <label><span>Цена</span><input value={tariffForm.price ?? 0} onChange={(e) => updateTariffForm('price', Number(e.target.value) || 0)} type="number" min={0} step="1" placeholder="490" /></label>
                <label><span>Валюта</span><input value={tariffForm.currency ?? 'RUB'} onChange={(e) => updateTariffForm('currency', e.target.value)} placeholder="RUB" maxLength={3} /></label>
                <label><span>Срок, дней</span><input value={tariffForm.durationDays ?? 30} onChange={(e) => updateTariffForm('durationDays', Number(e.target.value) || 0)} type="number" min={1} step="1" placeholder="30" /></label>
                <label><span>Устройств</span><input value={tariffForm.maxDevices ?? 3} onChange={(e) => updateTariffForm('maxDevices', Number(e.target.value) || 0)} type="number" min={1} step="1" placeholder="3" /></label>
                <label><span>Лимит трафика, ГБ</span><input value={tariffForm.trafficLimit ? Math.round(tariffForm.trafficLimit / 1024 / 1024 / 1024) : ''} onChange={(e) => updateTariffForm('trafficLimit', e.target.value ? Number(e.target.value) * 1024 * 1024 * 1024 : null)} type="number" min={0} step="1" placeholder="Без лимита" /></label>
              </div>
            </fieldset>
            <fieldset className="form-section">
              <legend>Публикация</legend>
              <div className="form-grid">
                <label><span>Порядок</span><input value={tariffForm.sortOrder ?? 100} onChange={(e) => updateTariffForm('sortOrder', Number(e.target.value) || 0)} type="number" min={0} step="1" placeholder="100" /></label>
                <label><span>Категория</span><input value={tariffForm.category ?? 'default'} onChange={(e) => updateTariffForm('category', e.target.value)} placeholder="default" maxLength={120} /></label>
                <label><span>Бейдж</span><input value={tariffForm.badge ?? ''} onChange={(e) => updateTariffForm('badge', e.target.value)} placeholder="Популярный, Выгодно, Семейный" maxLength={80} /></label>
                <label><span>Сценарий выдачи</span><select value={tariffForm.provisioningScenario ?? 'auto'} onChange={(e) => updateTariffForm('provisioningScenario', e.target.value)}><option value="auto">По умолчанию (auto)</option>{workScenarios.map((scenario) => <option key={scenario.id} value={scenario.key}>{scenario.name} ({scenario.key})</option>)}</select></label>
                <label><span>Тип тарифа</span><select value={tariffForm.tariffType ?? 'Personal'} onChange={(e) => updateTariffForm('tariffType', e.target.value)}><option value="Weekly">Неделя</option><option value="Monthly">Месяц</option><option value="Quarterly">Квартал</option><option value="SemiAnnual">Полгода</option><option value="Annual">Год</option><option value="Trial">Пробный</option><option value="Promo">Акция</option><option value="Personal">Персональный</option></select></label>
                <label><span>Показывать с</span><input type="datetime-local" value={toDateTimeLocalValue(tariffForm.visibleFrom)} onChange={(e) => updateTariffForm('visibleFrom', fromOptionalDateTimeLocalValue(e.target.value))} /></label>
                <label><span>Показывать до</span><input type="datetime-local" value={toDateTimeLocalValue(tariffForm.visibleTo)} onChange={(e) => updateTariffForm('visibleTo', fromOptionalDateTimeLocalValue(e.target.value))} /></label>
              </div>
              <label><span>Короткое описание</span><textarea value={tariffForm.description ?? ''} onChange={(e) => updateTariffForm('description', e.target.value)} placeholder="Коротко для карточки тарифа" rows={3} maxLength={500} /></label>
              <label><span>Полное описание</span><textarea value={tariffForm.fullDescription ?? ''} onChange={(e) => updateTariffForm('fullDescription', e.target.value)} placeholder="Подробное описание для публичной страницы" rows={4} maxLength={4000} /></label>
              <label><span>Преимущества, по одному в строке</span><textarea value={tariffFeaturesText} onChange={(e) => setTariffFeaturesText(e.target.value)} placeholder={'3 устройства\nАвтоматическая выдача\nQR-код в кабинете'} rows={5} /></label>
              <label><span>Текст после оплаты</span><textarea value={tariffForm.afterPaymentText ?? ''} onChange={(e) => updateTariffForm('afterPaymentText', e.target.value)} placeholder="Что увидит пользователь после покупки" rows={3} maxLength={2000} /></label>
              <div className="form-grid">
                <label><span>Разрешенные регионы</span><input value={tariffForm.allowedRegionsCsv ?? ''} onChange={(e) => updateTariffForm('allowedRegionsCsv', e.target.value)} placeholder="eu,us" maxLength={2000} /></label>
                <label><span>Группы серверов</span><input value={tariffForm.allowedNodeGroupsCsv ?? ''} onChange={(e) => updateTariffForm('allowedNodeGroupsCsv', e.target.value)} placeholder="default,premium" maxLength={2000} /></label>
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
            <FormValidationSummary errors={tariffForm !== defaultTariffForm || tariffFeaturesText ? tariffFormErrors : []} />
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={!token || !tariffFormChanged || tariffFormActionBusy || tariffFormErrors.length > 0} title={adminDisabledTitle} aria-busy={tariffFormActionBusy}>{editingTariffId ? 'Сохранить тариф' : 'Создать тариф'}</PrimaryButton>
              {editingTariffId && <PrimaryButton type="button" className="button-ghost" onClick={resetTariffForm}>Отменить редактирование</PrimaryButton>}
            </div>
          </form>
        </Card>
        <Card>
          <h3>Список тарифов</h3>
          <div className="list-stack">
            {tariffs.length === 0 && <EmptyState title="Тарифов нет" description="Создайте первый тариф, чтобы он появился на странице покупки." />}
            {tariffs.map((tariff) => <div key={tariff.id} className="list-item-vertical"><div className="item-head"><div><strong>{tariff.name}</strong><div className="muted">{tariff.description || '—'}</div><div className="muted">{tariff.durationDays} дней · {tariff.maxDevices} устройств · порядок {tariff.sortOrder ?? 0} · сценарий {tariff.provisioningScenario || 'auto'}</div><div className="muted">{parseTariffFeatures(tariff).join(' · ') || 'Преимущества не заполнены'}</div></div><div className="item-status"><strong>{tariff.price} {tariff.currency}</strong>{tariff.badge && <StatusBadge value={tariff.badge} />}<StatusBadge value={tariff.isActive === false ? 'Disabled' : 'Enabled'} /></div></div><div className="toolbar" hidden={!canWriteSection('tariffs')}><PrimaryButton className="button-secondary" disabled={isActionResourceBusy(tariffActionResourceKey(tariff.id))} onClick={() => editTariff(tariff)}>Редактировать</PrimaryButton>{tariff.isActive === false ? <PrimaryButton className="button-ghost" disabled={isActionResourceBusy(tariffActionResourceKey(tariff.id))} onClick={() => void handleToggleTariff(tariff)}>Включить</PrimaryButton> : <ConfirmButton className="button-secondary" disabled={isActionResourceBusy(tariffActionResourceKey(tariff.id))} message={`Выключить тариф "${tariff.name}"? Он исчезнет с публичной витрины и из Telegram.`} onConfirm={() => handleToggleTariff(tariff)}>Выключить</ConfirmButton>}<ConfirmButton className="button-danger" disabled={isActionResourceBusy(tariffActionResourceKey(tariff.id))} message={`Удалить тариф "${tariff.name}"? Если есть заказы или подписки, тариф будет архивирован и скрыт с витрины.`} onConfirm={() => handleDeleteTariff(tariff)}>Удалить</ConfirmButton></div></div>)}
          </div>
        </Card>
      </div>

      <div id="referrals" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('referrals')} hidden={activeSection !== 'referrals' || activeSectionLoadFailed}>
        <Card>
          <h3>{editingReferralProgramId ? 'Редактирование программы' : 'Новая реферальная программа'}</h3>
          <form hidden={!canWriteSection('referrals')} aria-busy={referralProgramFormActionBusy} onSubmit={(event) => { event.preventDefault(); void handleSaveReferralProgram() }}>
            <fieldset className="form-section">
              <legend>Публикация</legend>
              <div className="form-grid">
                <label><span>Название</span><input value={referralProgramForm.name} maxLength={160} onChange={(event) => updateReferralProgramForm('name', event.target.value)} required /></label>
                <label><span>Статус</span><select value={referralProgramForm.status} onChange={(event) => updateReferralProgramForm('status', event.target.value as ReferralProgramFormState['status'])}><option value="draft">Черновик</option><option value="active">Активна</option><option value="paused">Приостановлена</option><option value="archived">Архив</option></select></label>
                <label><span>Начало</span><input type="datetime-local" value={referralProgramForm.startAt} onChange={(event) => updateReferralProgramForm('startAt', event.target.value)} /></label>
                <label><span>Окончание</span><input type="datetime-local" value={referralProgramForm.endAt} onChange={(event) => updateReferralProgramForm('endAt', event.target.value)} /></label>
                <label><span>Минимальная сумма заказа</span><input type="number" min={0} step="0.01" value={referralProgramForm.minimumOrderAmount} onChange={(event) => updateReferralProgramForm('minimumOrderAmount', Number(event.target.value) || 0)} /></label>
              </div>
              <label className="checkbox-row"><input type="checkbox" checked={referralProgramForm.firstPurchaseOnly} onChange={(event) => updateReferralProgramForm('firstPurchaseOnly', event.target.checked)} /> Только первая покупка подписки</label>
              <div className="checkbox-grid" role="group" aria-label="Каналы продаж">
                {['Web', 'Telegram', 'Discord', 'Vk', 'WhatsApp', 'Email'].map((channel) => <label key={channel} className="checkbox-row"><input type="checkbox" checked={referralProgramForm.allowedChannels.includes(channel)} onChange={() => toggleReferralChannel(channel)} /> {formatStatusLabel(channel)}</label>)}
              </div>
            </fieldset>
            <fieldset className="form-section">
              <legend>Пригласивший пользователь</legend>
              <label className="checkbox-row"><input type="checkbox" checked={referralProgramForm.referrerEnabled} onChange={(event) => updateReferralProgramForm('referrerEnabled', event.target.checked)} /> Начислять вознаграждение</label>
              <div className="form-grid" hidden={!referralProgramForm.referrerEnabled}>
                <label><span>Тип</span><select value={referralProgramForm.referrerType} onChange={(event) => updateReferralProgramForm('referrerType', event.target.value)}>{!['bonus-days', 'cashback', 'discount'].includes(referralProgramForm.referrerType) && <option value={referralProgramForm.referrerType}>{referralProgramForm.referrerType}</option>}<option value="bonus-days">Бонусные дни</option><option value="cashback">Кэшбэк</option><option value="discount">Скидка</option></select></label>
                <label><span>Значение</span><input type="number" min={0.01} max={1_000_000} step="0.01" value={referralProgramForm.referrerValue} onChange={(event) => updateReferralProgramForm('referrerValue', Number(event.target.value) || 0)} /></label>
                <label><span>Единица</span><input value={referralProgramForm.referrerUnit} maxLength={32} onChange={(event) => updateReferralProgramForm('referrerUnit', event.target.value)} /></label>
              </div>
              <label className="checkbox-row" hidden={!referralProgramForm.referrerEnabled}><input type="checkbox" checked={referralProgramForm.referrerAutoApprove} onChange={(event) => updateReferralProgramForm('referrerAutoApprove', event.target.checked)} /> Подтверждать автоматически</label>
            </fieldset>
            <fieldset className="form-section">
              <legend>Приглашенный пользователь</legend>
              <label className="checkbox-row"><input type="checkbox" checked={referralProgramForm.referredEnabled} onChange={(event) => updateReferralProgramForm('referredEnabled', event.target.checked)} /> Начислять вознаграждение</label>
              <div className="form-grid" hidden={!referralProgramForm.referredEnabled}>
                <label><span>Тип</span><select value={referralProgramForm.referredType} onChange={(event) => updateReferralProgramForm('referredType', event.target.value)}>{!['bonus-days', 'cashback', 'discount'].includes(referralProgramForm.referredType) && <option value={referralProgramForm.referredType}>{referralProgramForm.referredType}</option>}<option value="bonus-days">Бонусные дни</option><option value="cashback">Кэшбэк</option><option value="discount">Скидка</option></select></label>
                <label><span>Значение</span><input type="number" min={0.01} max={1_000_000} step="0.01" value={referralProgramForm.referredValue} onChange={(event) => updateReferralProgramForm('referredValue', Number(event.target.value) || 0)} /></label>
                <label><span>Единица</span><input value={referralProgramForm.referredUnit} maxLength={32} onChange={(event) => updateReferralProgramForm('referredUnit', event.target.value)} /></label>
              </div>
              <label className="checkbox-row" hidden={!referralProgramForm.referredEnabled}><input type="checkbox" checked={referralProgramForm.referredAutoApprove} onChange={(event) => updateReferralProgramForm('referredAutoApprove', event.target.checked)} /> Подтверждать автоматически</label>
            </fieldset>
            <FormValidationSummary errors={referralProgramForm !== defaultReferralProgramForm ? referralProgramFormErrors : []} />
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={!referralProgramFormChanged || referralProgramFormActionBusy || referralProgramFormErrors.length > 0} aria-busy={referralProgramFormActionBusy}>{editingReferralProgramId ? 'Сохранить программу' : 'Создать программу'}</PrimaryButton>
              {editingReferralProgramId && <PrimaryButton type="button" className="button-ghost" onClick={resetReferralProgramForm}>Отменить редактирование</PrimaryButton>}
            </div>
          </form>
        </Card>
        <Card>
          <h3>Программы</h3>
          <div className="list-stack">
            {referralPrograms.length === 0 && <EmptyState title="Программ нет" description="Создайте программу и активируйте ее для начислений после первой покупки." />}
            {referralPrograms.map((program) => <div key={program.id} className="list-item-vertical"><div className="item-head"><div><strong>{program.name}</strong><div className="muted">Период: {formatDate(program.startAt)} - {formatDate(program.endAt)}</div><div className="muted">Обновлена: {formatDate(program.updatedAt)}</div></div><StatusBadge value={program.status} /></div><div className="toolbar" hidden={!canWriteSection('referrals')}><PrimaryButton className="button-secondary" disabled={referralProgramFormActionBusy} onClick={() => editReferralProgram(program)}>Редактировать</PrimaryButton></div></div>)}
          </div>
        </Card>
        <Card>
          <h3>Начисления</h3>
          <div className="list-stack">
            {referralRewards.length === 0 && <EmptyState title="Начислений нет" description="Начисления появятся после успешных покупок по реферальным приглашениям." />}
            {referralRewards.slice(0, 50).map((reward) => <div key={reward.id} className="list-item"><span><strong>{formatReferralRewardType(reward.type)}</strong> · {formatReferralRewardValue(reward.value, reward.currencyOrUnit)}<span className="muted"> · получатель {shortId(reward.userId)} · программа {shortId(reward.referralProgramId)} · {formatDate(reward.createdAt)}</span></span><StatusBadge value={reward.status} /></div>)}
          </div>
        </Card>
      </div>

      <div id="subscriptions" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('subscriptions')} hidden={activeSection !== 'subscriptions' || activeSectionLoadFailed}>
        <Card>
          <h3>Подписки</h3>
          <div className="list-stack">
            {subscriptions.length === 0 && <EmptyState title="Подписок нет" description="После успешной оплаты подписка появится здесь." />}
            {subscriptions.slice(0, 12).map((subscription) => {
              const isActionBusy = actionBusyResourceKeys.has(subscriptionActionResourceKey(subscription.id))
              const actionAvailability = getAdminSubscriptionActionAvailability(subscription, adminAccessNow)
              const migrationOptions = subscriptionMigrationOptions(subscription)
              const canMigrate = Boolean(adminSession?.capabilities.vpnManage && actionAvailability.canMigrate)
              return (
                <div id={`subscription-${subscription.id}`} key={subscription.id} className="list-item-vertical">
                  <div className="item-head">
                    <div>
                      <strong>{subscription.tariffName || shortId(subscription.tariffId)}</strong>
                      <div className="muted">Пользователь: {shortId(subscription.userId)} · источник: {subscription.sourceChannel ? formatStatusLabel(subscription.sourceChannel) : '—'} · период {formatDate(subscription.startAt)} - {formatDate(subscription.endAt)}</div>
                      <div className="muted">Доступ: {shortId(subscription.currentAccessId)} · сервер: {shortId(subscription.currentServerId)} · платеж: {shortId(subscription.lastPaymentId)} · продлений: {subscription.renewalCount ?? 0}</div>
                      <div className="muted">Льготный период до: {formatDate(subscription.gracePeriodEndAt)} · причина ограничения: {subscription.blockReason || '—'}</div>
                      {subscription.lifecycleLastError && <div className="error-text">Ошибка отключения: {subscription.lifecycleLastError} · попытка {subscription.lifecycleAttemptCount ?? 0} · повтор {formatDate(subscription.lifecycleNextAttemptAt)}</div>}
                    </div>
                    <div className="item-status">
                      <StatusBadge value={subscription.status} />
                      <StatusBadge value={subscription.currentAccessId ? 'Access linked' : 'No access'} />
                      {actionAvailability.isEffectivelyExpired && <StatusBadge value="Access expired" />}
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
                    <PrimaryButton className="button-secondary" disabled={isActionBusy || !actionAvailability.canSync} title={actionAvailability.canSync ? undefined : getAdminSubscriptionActionBlocker(subscription, 'sync', adminAccessNow) ?? undefined} onClick={() => void handleSubscriptionAction(subscription, 'sync')}>Синхронизировать доступ</PrimaryButton>
                    {actionAvailability.canToggleBlock && <ConfirmButton className="button-secondary" disabled={isActionBusy} message={`${subscription.status === 'Blocked' ? 'Разблокировать' : 'Заблокировать'} подписку? Это влияет на доступ пользователя.`} onConfirm={() => handleSubscriptionAction(subscription, subscription.status === 'Blocked' ? 'unblock' : 'block')}>{subscription.status === 'Blocked' ? 'Разблокировать' : 'Заблокировать'}</ConfirmButton>}
                    {actionAvailability.canCancel && <ConfirmButton className="button-danger" disabled={isActionBusy} message="Отменить подписку без возможности восстановления? VPN-доступ будет отозван и удален с сервера, а занятый слот освободится." onConfirm={() => handleSubscriptionAction(subscription, 'cancel')}>Отменить</ConfirmButton>}
                  </div>}
                  {canMigrate && <div className="toolbar">
                    <select aria-label={`Целевой сервер для миграции подписки ${shortId(subscription.id)}`} value={subscriptionMigrationTargets[subscription.id] ?? ''} onChange={(event) => setSubscriptionMigrationTargets((current) => ({ ...current, [subscription.id]: event.target.value }))}>
                      <option value="">Сервер для миграции</option>
                      <option value="auto">Автовыбор готового сервера</option>
                      {migrationOptions.map((server) => <option key={server.id} value={server.id}>{server.name} · {server.region} · {server.usedCapacity}/{server.capacity}</option>)}
                    </select>
                    <ConfirmButton className="button-secondary" disabled={isActionBusy || !subscriptionMigrationTargets[subscription.id]} message="Перенести VPN-доступ подписки на другой сервер? Клиент будет создан на целевой панели, удален с исходной, а подписка и ключ будут переключены на новый сервер." onConfirm={() => handleMigrateSubscription(subscription)}>Перенести</ConfirmButton>
                  </div>}
                </div>
              )
            })}
          </div>
        </Card>
      </div>

      <div id="vpn" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('vpn')} hidden={activeSection !== 'vpn' || activeSectionLoadFailed}>
        <Card>
          <h3>VPN-доступы</h3>
          <div className="list-stack">
            {accessCredentials.length === 0 && <EmptyState title="VPN-доступы пока не созданы" description="После оплаты здесь появится ссылка подключения, статус и история синхронизаций." />}
            {accessCredentials.slice(0, 12).map((access) => {
              const terminalReason = getAdminAccessTerminalReason(access, adminAccessNow)
              const isTerminal = Boolean(terminalReason)
              const isActionBusy = actionBusyResourceKeys.has(accessActionResourceKey(access.id))
              const canDisableExpiredAccess = canWriteSection('vpn')
                && isAdminAccessExpired(access, adminAccessNow)
                && access.subscriptionStatus !== 'Cancelled'
                && access.status !== 'Disabled'
                && access.status !== 'Revoked'
              return <div key={access.id} className="list-item-vertical">
                <div className="item-head">
                  <strong>{formatStatusLabel(access.providerType)} · {isTerminal ? shortId(access.id) : (access.providerAccessId || shortId(access.id))}</strong>
                  <StatusBadge value={access.status} />
                </div>
                <div className="muted">Пользователь: {shortId(access.userId)} · подписка: {shortId(access.subscriptionId)} · сервер: {access.serverName || shortId(access.serverId)} · до: {formatDate(access.expiryDate)}</div>
                <div className="muted">Последняя синхронизация: {formatDate(access.lastSyncedAt)} · версия: {access.revision ?? 0} · клиент провайдера: {isTerminal ? 'скрыт' : (access.providerAccessId || '—')}</div>
                {isTerminal
                  ? <p className="safe-note" role="status">{terminalReason}</p>
                  : access.accessUri && <CodeBlock>{access.accessUri}</CodeBlock>}
                {access.history && access.history.length > 0 && <div className="muted">История: {access.history.slice(0, 3).map((h) => `${formatAdminDisplayLabel(h.eventType)} ${formatDate(h.createdAt)}`).join(' · ')}</div>}
                {!isTerminal && adminQrSvgs[access.id] && <QrCodePreview svg={adminQrSvgs[access.id]} label={`QR-код доступа ${access.id}`} />}
                {!isTerminal && <div className="toolbar">
                  <CopyButton value={access.accessUri} label="Скопировать URI" disabled={!access.accessUri} />
                  <PrimaryButton disabled={!access.accessUri || isActionBusy} aria-busy={isActionBusy} onClick={() => void handleAdminAccessQr(access)}>Показать QR</PrimaryButton>
                  {canWriteSection('vpn') && <>
                    {access.status === 'Disabled'
                      ? <PrimaryButton disabled={isActionBusy} className="button-secondary" onClick={() => void handleAccessAction(access, true)}>Включить</PrimaryButton>
                      : <ConfirmButton disabled={isActionBusy} className="button-secondary" message="Отключить VPN-доступ? Пользователь потеряет возможность подключаться." onConfirm={() => handleAccessAction(access, false)}>Отключить</ConfirmButton>}
                    <PrimaryButton disabled={isActionBusy} aria-busy={isActionBusy} onClick={() => void handleAccessSync(access)}>Синхронизировать</PrimaryButton>
                    <ConfirmButton disabled={isActionBusy} message="Необратимо обнулить счётчики трафика у VPN-провайдера? При сетевой неопределённости доступ получит статус SyncRequired для ручной сверки." onConfirm={() => handleAccessResetTraffic(access)}>Сбросить трафик</ConfirmButton>
                  </>}
                </div>}
                {canDisableExpiredAccess && <div className="toolbar">
                  <ConfirmButton disabled={isActionBusy} className="button-secondary" message="Отключить истёкший VPN-доступ у провайдера?" onConfirm={() => handleAccessAction(access, false)}>Отключить у провайдера</ConfirmButton>
                </div>}
              </div>
            })}
          </div>
        </Card>
      </div>

      <div id="nodes" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('nodes')} hidden={activeSection !== 'nodes' || activeSectionLoadFailed}>
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
                    <div className="muted">Панель: {server.panelBaseUrl || '—'} · SSH {server.sshUser ?? 'root'}:{server.sshPort ?? 22} · авторизация: {formatAdminDisplayLabel(server.sshAuthMethod || 'not_configured')} · доступы: {server.sshCredentialConfigured ? 'заданы' : 'не заданы'}</div>
                    <div className="muted">Подготовка VPS: {provisioningDeployModeLabel(serverProvisioningMode(server))} · риск {provisioningRiskLabel(server.provisioningRiskLevel)} · рабочее развёртывание {server.liveDeployAllowed ? 'разрешено' : 'закрыто'} · {server.provisioningNextAction || server.provisioningOperatorWarning || 'сначала выполните предварительную проверку'}</div>
                    <div className="muted">Последняя проверка: {formatDate(server.lastHealthCheckAt)} · задержка {server.lastHealthLatencyMs ?? 0} мс · {server.lastHealthError ? formatAdminDisplayLabel(server.lastHealthError) : 'ошибок нет'}</div>
                  </div>
                  <div className="item-status"><StatusBadge value={server.status} /><StatusBadge value={server.healthStatus} /><StatusBadge value={provisioningRiskBadge(server.provisioningRiskLevel)} /></div>
                </div>
                {server.provisioningOperatorWarning && <div className="safe-note">{server.provisioningOperatorWarning}</div>}
                <div className="toolbar" hidden={!canWriteSection('nodes')}>
                  <PrimaryButton className="button-secondary" disabled={isActionResourceBusy(serverActionResourceKey(server.id))} onClick={() => editServer(server)}>Редактировать</PrimaryButton>
                  <PrimaryButton disabled={isActionResourceBusy(serverActionResourceKey(server.id))} onClick={() => void handleCheckServerHealth(server)}>Health-check</PrimaryButton>
                  <PrimaryButton disabled={server.status === 'Archived' || isActionResourceBusy(serverActionResourceKey(server.id))} onClick={() => void handleQueuePrecheck(server)}>Проверить VPS</PrimaryButton>
                  <ConfirmButton className="button-danger" disabled={!serverProvisioningCanDeploy(server) || isActionResourceBusy(serverActionResourceKey(server.id))} message={`Запустить подготовку сервера "${server.name}"? Режим: ${provisioningDeployModeLabel(serverProvisioningMode(server))}. ${server.provisioningOperatorWarning || 'Проверьте сервер перед запуском.'}`} onConfirm={() => handleQueueProvision(server)}>Подготовить</ConfirmButton>
                  <ConfirmButton className="button-secondary" disabled={server.status === 'Archived' || isActionResourceBusy(serverActionResourceKey(server.id))} message="Перевести сервер в обслуживание? Новые пользователи не должны попадать на него." onConfirm={() => handleServerMode(server, 'maintenance')}>В обслуживание</ConfirmButton>
                  <PrimaryButton className="button-secondary" disabled={server.status === 'Archived' || isActionResourceBusy(serverActionResourceKey(server.id))} onClick={() => void handleServerMode(server, 'ready')}>Вернуть в работу</PrimaryButton>
                  <ConfirmButton className="button-secondary" disabled={server.status === 'Archived' || isActionResourceBusy(serverActionResourceKey(server.id))} message={`${server.isAvailableForNewUsers ? 'Закрыть набор на сервер' : 'Открыть набор на сервер'}? Это изменит распределение новых пользователей.`} onConfirm={() => handleServerMode(server, server.isAvailableForNewUsers ? 'drain' : 'allocate')}>{server.isAvailableForNewUsers ? 'Закрыть набор' : 'Открыть набор'}</ConfirmButton>
                  <ConfirmButton className="button-secondary" disabled={server.status === 'Disabled' || server.status === 'Archived' || isActionResourceBusy(serverActionResourceKey(server.id))} message={`Отключить сервер "${server.name}"? Новые подключения и автоматическое распределение будут закрыты.`} onConfirm={() => handleServerMode(server, 'disable')}>Отключить</ConfirmButton>
                  <ConfirmButton className="button-danger" disabled={isActionResourceBusy(serverActionResourceKey(server.id))} message={`Удалить сервер "${server.name}"? При наличии подписок, VPN-доступов, запусков подготовки, health-check или миграций он будет архивирован.`} onConfirm={() => handleDeleteServer(server)}>Удалить</ConfirmButton>
                </div>
              </div>
            ))}
          </div>
        </Card>
        <Card>
          <h3>{editingServerId ? 'Редактировать VPN-сервер' : 'Добавить VPN-сервер'}</h3>
          <form hidden={!canWriteSection('nodes')} aria-busy={serverFormActionBusy} onSubmit={(event) => { event.preventDefault(); void handleSaveServer() }}>
            <fieldset className="form-section">
              <legend>Идентификация сервера</legend>
              <div className="form-grid">
                <label><span>Название</span><input value={serverForm.name} onChange={(e) => updateServerForm('name', e.target.value)} placeholder="nl-01" maxLength={200} required /></label>
                <label><span>Host или DNS</span><input value={serverForm.host} onChange={(e) => updateServerForm('host', e.target.value)} placeholder="vpn.example.com" maxLength={253} required /></label>
                <label><span>IP-адрес</span><input value={serverForm.ipAddress} onChange={(e) => updateServerForm('ipAddress', e.target.value)} placeholder="203.0.113.10" maxLength={64} /></label>
                <label><span>Провайдер</span><input value={serverForm.provider} onChange={(e) => updateServerForm('provider', e.target.value)} placeholder="hetzner" maxLength={120} /></label>
                <label><span>Регион</span><input value={serverForm.region} onChange={(e) => updateServerForm('region', e.target.value)} placeholder="eu" maxLength={120} /></label>
                <label><span>Страна</span><input value={serverForm.country} onChange={(e) => updateServerForm('country', e.target.value)} placeholder="NL" maxLength={80} /></label>
                <label><span>Дата-центр</span><input value={serverForm.datacenter} onChange={(e) => updateServerForm('datacenter', e.target.value)} placeholder="fsn1" maxLength={120} /></label>
                <label><span>Емкость</span><input value={serverForm.capacity} onChange={(e) => updateServerForm('capacity', Number(e.target.value) || 0)} placeholder="5000" type="number" min={1} step="1" /></label>
                <label><span>Приоритет</span><input value={serverForm.priority} onChange={(e) => updateServerForm('priority', Number(e.target.value) || 0)} placeholder="100" type="number" min={1} step="1" /></label>
                <label><span>Протоколы</span><select value={serverForm.supportedProtocolsCsv ?? 'vless,vmess,trojan'} onChange={(e) => updateServerForm('supportedProtocolsCsv', e.target.value)}><option value="vless,vmess,trojan">VLESS, VMess, Trojan</option><option value="vless">VLESS</option><option value="vmess">VMess</option><option value="trojan">Trojan</option><option value="vless,vmess">VLESS, VMess</option><option value="vless,trojan">VLESS, Trojan</option><option value="vmess,trojan">VMess, Trojan</option></select></label>
                <label><span>Теги</span><input value={serverForm.tagsCsv ?? ''} onChange={(e) => updateServerForm('tagsCsv', e.target.value)} placeholder="tier:premium,city:amsterdam" maxLength={2000} /></label>
              </div>
            </fieldset>
            <fieldset className="form-section">
              <legend>SSH и режим запуска</legend>
              <div className="form-grid">
                <label><span>SSH-пользователь</span><input value={serverForm.sshUser ?? ''} onChange={(e) => updateServerForm('sshUser', e.target.value)} placeholder="root" maxLength={64} /></label>
                <label><span>SSH-порт</span><input value={serverForm.sshPort} onChange={(e) => updateServerForm('sshPort', Number(e.target.value) || 22)} placeholder="22" type="number" min={1} max={65535} step="1" /></label>
                <label><span>Метод SSH</span><select value={serverForm.sshAuthMethod ?? 'ssh_key'} onChange={(e) => updateServerForm('sshAuthMethod', e.target.value)}><option value="ssh_key">SSH-ключ</option><option value="password">Пароль</option></select></label>
                <SecretField label="SSH-доступ" value={serverForm.sshCredential ?? ''} maxLength={16000} onChange={(value) => updateServerForm('sshCredential', value)} />
                <label><span>Режим запуска</span><select value={serverForm.validationMode ? 'true' : 'false'} onChange={(e) => updateServerForm('validationMode', e.target.value === 'true')}><option value="true">Проверка без реального деплоя</option><option value="false">Рабочий кандидат</option></select></label>
              </div>
            </fieldset>
            <fieldset className="form-section">
              <legend>Панель управления</legend>
              <div className="form-grid">
                <label><span>URL панели</span><input value={serverForm.panelBaseUrl ?? ''} onChange={(e) => updateServerForm('panelBaseUrl', e.target.value)} placeholder="https://panel.example.com:2053" type="url" inputMode="url" maxLength={2000} /></label>
                <label><span>Логин панели</span><input value={serverForm.panelUsername ?? ''} onChange={(e) => updateServerForm('panelUsername', e.target.value)} placeholder="admin" maxLength={200} /></label>
                <SecretField label="Пароль панели" value={serverForm.panelPassword ?? ''} maxLength={4096} onChange={(value) => updateServerForm('panelPassword', value)} />
                <label><span>Inbound ID</span><input value={serverForm.panelInboundId ?? ''} onChange={(e) => updateServerForm('panelInboundId', e.target.value ? Number(e.target.value) : null)} type="number" min={1} step="1" /></label>
                <label><span>Публичный hostname</span><input value={serverForm.publicHostname ?? ''} onChange={(e) => updateServerForm('publicHostname', e.target.value)} placeholder="vpn.example.com" maxLength={253} /></label>
                <label><span>Публичный порт</span><input value={serverForm.publicPort} onChange={(e) => updateServerForm('publicPort', Number(e.target.value) || 0)} type="number" min={1} max={65535} step="1" /></label>
              </div>
            </fieldset>
            <p className="muted">SSH-доступ защищается API и не возвращается обратно. Проверочный режим не выполняет реальный SSH-деплой.</p>
            <FormValidationSummary errors={serverForm !== defaultServerForm ? serverFormErrors : []} />
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={serverFormActionBusy || !token || serverFormErrors.length > 0} title={adminDisabledTitle} aria-busy={serverFormActionBusy}>{editingServerId ? 'Сохранить сервер' : 'Создать сервер'}</PrimaryButton>
              {editingServerId && <PrimaryButton type="button" className="button-ghost" onClick={cancelServerEdit}>Отменить редактирование</PrimaryButton>}
            </div>
          </form>
        </Card>
      </div>

      <div id="panels" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('panels')} hidden={activeSection !== 'panels' || activeSectionLoadFailed}>
        <Card>
          <h3>{editingVpnPanelId ? 'Редактировать 3x-ui панель' : '3x-ui панели'}</h3>
          <p className="safe-note">В проверочном режиме тест и синхронизация идут через безопасный путь без реального подключения к 3x-ui.</p>
          <form hidden={!canWriteSection('panels')} aria-busy={vpnPanelFormActionBusy} onSubmit={(event) => { event.preventDefault(); void handleSaveVpnPanel() }}>
            <fieldset className="form-section">
              <legend>Доступ к панели</legend>
              <div className="form-grid">
                <label><span>Название панели</span><input value={vpnPanelForm.name} onChange={(e) => updateVpnPanelForm('name', e.target.value)} placeholder="main-3xui" maxLength={200} required /></label>
                <label><span>Адрес панели</span><input value={vpnPanelForm.baseUrl} onChange={(e) => updateVpnPanelForm('baseUrl', e.target.value)} placeholder="https://panel.example.com:2053" type="url" inputMode="url" maxLength={2048} required /></label>
                <label><span>Логин</span><input value={vpnPanelForm.login} onChange={(e) => updateVpnPanelForm('login', e.target.value)} placeholder="admin" maxLength={200} /></label>
                <PasswordField label="Пароль панели" value={vpnPanelForm.password ?? ''} maxLength={4096} onChange={(value) => updateVpnPanelForm('password', value)} placeholder={editingVpnPanelId ? 'Оставьте пустым, чтобы сохранить текущий пароль' : 'Хранится зашифрованным'} autoComplete="new-password" />
              </div>
            </fieldset>
            <fieldset className="form-section">
              <legend>Распределение нагрузки</legend>
              <div className="form-grid">
                <label><span>Регион</span><input value={vpnPanelForm.region} onChange={(e) => updateVpnPanelForm('region', e.target.value)} placeholder="eu" maxLength={120} /></label>
                <label><span>Емкость</span><input value={vpnPanelForm.capacity} onChange={(e) => updateVpnPanelForm('capacity', Number(e.target.value) || 0)} placeholder="5000" type="number" min={1} step="1" /></label>
                <label><span>Проверка SSL</span><select value={vpnPanelForm.sslVerificationMode} onChange={(e) => updateVpnPanelForm('sslVerificationMode', e.target.value)}><option value="Strict">Строгая</option><option value="AllowSelfSigned">Самоподписанный</option><option value="Disabled">Отключена</option></select></label>
                <label><span>Вариант API</span><select value={vpnPanelForm.apiVariant} onChange={(e) => updateVpnPanelForm('apiVariant', e.target.value)}><option value="X3UiOfficial">X3UiOfficial</option><option value="ThreeXUi">ThreeXUi</option><option value="LegacyXUi">LegacyXUi</option><option value="Custom">Custom</option></select></label>
              </div>
              <label className="checkbox-row"><input checked={vpnPanelForm.autoCreateInbound} onChange={(e) => updateVpnPanelForm('autoCreateInbound', e.target.checked)} type="checkbox" /> Автоматически создавать inbound при выдаче доступа</label>
              <label><span>Шаблон inbound JSON</span><textarea value={vpnPanelForm.defaultInboundTemplateJson} onChange={(e) => updateVpnPanelForm('defaultInboundTemplateJson', e.target.value)} rows={4} maxLength={32768} placeholder='{"remark":"default-vless","protocol":"vless","port":443}' /></label>
            </fieldset>
            <FormValidationSummary errors={vpnPanelForm !== defaultVpnPanelForm ? vpnPanelFormErrors : []} />
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={!vpnPanelFormChanged || vpnPanelFormActionBusy || !token || vpnPanelFormErrors.length > 0} title={adminDisabledTitle} aria-busy={vpnPanelFormActionBusy}>{editingVpnPanelId ? 'Сохранить панель' : 'Добавить панель'}</PrimaryButton>
              {editingVpnPanelId && <PrimaryButton type="button" className="button-ghost" onClick={cancelVpnPanelEdit}>Отменить редактирование</PrimaryButton>}
            </div>
          </form>
          {vpnPanels.length === 0 && <EmptyState title="3x-ui панели не добавлены" description="Добавьте панель, чтобы управлять inbound-правилами, клиентами и синхронизацией." />}
          <div className="list-stack mt-12">{vpnPanels.map((panel) => <div key={panel.id} className={`list-item-vertical${selectedVpnPanelId === panel.id ? ' selected-item' : ''}`}><div className="item-head"><div><strong>{panel.name}</strong><div className="muted">{panel.baseUrl} · логин {panel.login ? 'задан' : 'пусто'} · {panel.apiVariant} · SSL {formatAdminDisplayLabel(panel.sslVerificationMode)}</div><div className="muted">Емкость {panel.usedCapacity}/{panel.capacity} · авто inbound: {panel.autoCreateInbound ? 'включен' : 'выключен'} · версия {panel.version || 'неизвестна'} · проверка {formatDate(panel.lastHealthCheckAt)} · синхронизация {formatDate(panel.lastSyncAt)}</div>{panel.lastError && <div className="error-text">Последняя ошибка: {panel.lastError}</div>}</div><div className="item-status"><StatusBadge value={panel.status} /><StatusBadge value={panel.healthStatus} /></div></div><div className="toolbar"><PrimaryButton className={selectedVpnPanelId === panel.id ? 'button-secondary' : 'button-ghost'} onClick={() => selectVpnPanel(panel.id)}>{selectedVpnPanelId === panel.id ? 'Открыто' : 'Открыть'}</PrimaryButton>{canWriteSection('panels') && <><PrimaryButton className="button-secondary" disabled={isActionResourceBusy(vpnPanelActionResourceKey(panel.id))} onClick={() => editVpnPanel(panel)}>Редактировать</PrimaryButton><PrimaryButton className="button-secondary" disabled={isActionResourceBusy(vpnPanelActionResourceKey(panel.id))} onClick={() => void handleTestVpnPanel(panel.id)}>Проверить</PrimaryButton><PrimaryButton disabled={isActionResourceBusy(vpnPanelActionResourceKey(panel.id))} onClick={() => void handleSyncVpnPanel(panel.id)}>Синхронизировать</PrimaryButton>{panel.status === 'Disabled' ? <PrimaryButton className="button-ghost" disabled={isActionResourceBusy(vpnPanelActionResourceKey(panel.id))} onClick={() => void handleSetVpnPanelStatus(panel, 'Active')}>Включить</PrimaryButton> : <ConfirmButton className="button-secondary" disabled={isActionResourceBusy(vpnPanelActionResourceKey(panel.id))} message={`Отключить 3x-ui панель "${panel.name}"? Новые выдачи не должны выбирать эту панель.`} onConfirm={() => handleSetVpnPanelStatus(panel, 'Disabled')}>Отключить</ConfirmButton>}<ConfirmButton className="button-danger" disabled={isActionResourceBusy(vpnPanelActionResourceKey(panel.id))} message={`Удалить 3x-ui панель "${panel.name}"? Если есть inbound-ы, клиенты или история синхронизаций, панель будет отключена и сохранена.`} onConfirm={() => handleDeleteVpnPanel(panel)}>Удалить</ConfirmButton></>}</div></div>)}</div>
        </Card>
        <Card>
          <h3>Детали панели</h3>
          <label><span>Панель</span><select value={selectedVpnPanelId} onChange={(e) => selectVpnPanel(e.target.value)}><option value="">Не выбрана</option>{vpnPanels.map((panel) => <option key={panel.id} value={panel.id}>{panel.name}</option>)}</select></label>
          {vpnPanelDetailsLoading && <LoadingBlock label="Загружаем детали VPN-панели..." />}
          {vpnPanelDetailsError && selectedVpnPanelId && (
            <div className="toast-error detail-load-error" role="alert">
              <p>Не удалось загрузить детали VPN-панели: {vpnPanelDetailsError}</p>
              <PrimaryButton type="button" className="button-secondary" disabled={vpnPanelDetailsLoading} aria-busy={vpnPanelDetailsLoading} onClick={() => void loadVpnPanelDetails(selectedVpnPanelId, token, sessionOperationId.current)}>
                Повторить загрузку деталей
              </PrimaryButton>
            </div>
          )}
          {!selectedVpnPanelId && <p className="muted">Выберите панель.</p>}
          {selectedVpnPanelId && !vpnPanelDetailsLoading && !vpnPanelDetailsError && <>
          <h4>Inbound-правила</h4>
          <form hidden={!canWriteSection('panels')} aria-busy={inboundFormActionBusy} onSubmit={(event) => { event.preventDefault(); void handleSaveInbound() }}>
            <fieldset className="form-section">
              <legend>{editingInboundId ? 'Редактирование inbound-правила' : 'Параметры нового inbound-правила'}</legend>
              <div className="form-grid">
                <label><span>Название inbound-правила</span><input value={inboundForm.name} onChange={(e) => updateInboundForm('name', e.target.value)} placeholder="default-vless" maxLength={200} required /></label>
                <label><span>Протокол</span><select value={inboundForm.protocol} onChange={(e) => updateInboundForm('protocol', e.target.value)}><option value="vless">VLESS</option><option value="vmess">VMess</option><option value="trojan">Trojan</option></select></label>
                <label><span>Порт</span><input value={inboundForm.port} onChange={(e) => updateInboundForm('port', Number(e.target.value) || 0)} placeholder="443" type="number" min={1} max={65535} step="1" /></label>
                <label><span>Listen</span><input value={inboundForm.listen} onChange={(e) => updateInboundForm('listen', e.target.value)} placeholder="0.0.0.0 или пусто" maxLength={255} /></label>
                <label><span>Емкость</span><input value={inboundForm.capacity} onChange={(e) => updateInboundForm('capacity', Number(e.target.value) || 0)} placeholder="5000" type="number" min={1} step="1" /></label>
              </div>
              <div className="form-grid mt-12">
                <label className="checkbox-row"><input checked={inboundForm.isActive} onChange={(e) => setInboundForm((current) => ({ ...current, isActive: e.target.checked, isDefault: e.target.checked ? current.isDefault : false }))} type="checkbox" /> Активен и доступен для выдачи</label>
                <label className="checkbox-row"><input checked={inboundForm.isDefault} disabled={!inboundForm.isActive} onChange={(e) => updateInboundForm('isDefault', e.target.checked)} type="checkbox" /> Основной inbound для панели</label>
              </div>
              <label className="mt-12"><span>settingsJson</span><textarea value={inboundForm.settingsJson} onChange={(e) => updateInboundForm('settingsJson', e.target.value)} rows={4} maxLength={32768} spellCheck={false} placeholder='{"clients":[]}' /></label>
              <label><span>streamSettingsJson</span><textarea value={inboundForm.streamSettingsJson} onChange={(e) => updateInboundForm('streamSettingsJson', e.target.value)} rows={4} maxLength={32768} spellCheck={false} placeholder='{"network":"tcp","security":"tls"}' /></label>
              <label><span>sniffingJson</span><textarea value={inboundForm.sniffingJson} onChange={(e) => updateInboundForm('sniffingJson', e.target.value)} rows={3} maxLength={32768} spellCheck={false} placeholder="{}" /></label>
            </fieldset>
            <FormValidationSummary errors={inboundFormErrors} />
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={!inboundFormChanged || inboundFormErrors.length > 0 || inboundFormActionBusy} aria-busy={inboundFormActionBusy}>{editingInboundId ? 'Сохранить inbound-правило' : 'Создать inbound-правило'}</PrimaryButton>
              {editingInboundId && <PrimaryButton type="button" className="button-ghost" onClick={cancelInboundEdit}>Отменить редактирование</PrimaryButton>}
            </div>
          </form>
          <div className="list-stack mt-12">{vpnInbounds.map((inbound) => <div key={inbound.id} className="list-item-vertical"><div className="item-head"><div><strong>{inbound.name}</strong><div className="muted">{inbound.protocol}:{inbound.port} · внешний ID {inbound.externalInboundId} · емкость {inbound.usedCapacity}/{inbound.capacity}</div><div className="muted">stream: {inbound.streamSettingsJson}</div></div><div className="item-status"><StatusBadge value={inbound.isActive ? 'Active' : 'Inactive'} />{inbound.isDefault && <StatusBadge value="Default" />}</div></div><div className="toolbar" hidden={!canWriteSection('panels')}><PrimaryButton className="button-secondary" disabled={isActionResourceBusy(vpnPanelActionResourceKey(inbound.vpnPanelId), vpnInboundActionResourceKey(inbound.id))} onClick={() => editInbound(inbound)}>Редактировать</PrimaryButton>{!inbound.isDefault && inbound.isActive && <PrimaryButton disabled={isActionResourceBusy(vpnPanelActionResourceKey(inbound.vpnPanelId), vpnInboundActionResourceKey(inbound.id))} onClick={() => void handleSetDefaultInbound(inbound.id)}>Сделать основным</PrimaryButton>}{inbound.isActive ? <ConfirmButton className="button-secondary" disabled={isActionResourceBusy(vpnPanelActionResourceKey(inbound.vpnPanelId), vpnInboundActionResourceKey(inbound.id))} message={`Выключить inbound-правило "${inbound.name}"? Новые VPN-доступы не будут использовать его для выдачи.`} onConfirm={() => handleToggleInboundActive(inbound)}>Выключить</ConfirmButton> : <PrimaryButton className="button-ghost" disabled={isActionResourceBusy(vpnPanelActionResourceKey(inbound.vpnPanelId), vpnInboundActionResourceKey(inbound.id))} onClick={() => void handleToggleInboundActive(inbound)}>Включить</PrimaryButton>}</div></div>)}</div>
          <h4>Клиенты, здоровье и синхронизация</h4>
          <div className="list-stack">{vpnClients.map((client) => {
            const inbound = vpnInbounds.find((item) => item.id === client.vpnInboundId)
            const migrationOptionGroups = migrationOptionGroupsForClient(client)
            const migrationOptionsCount = migrationOptionGroups.reduce((total, group) => total + group.inbounds.length, 0)
            const selectedMigrationInbound = vpnMigrationInbounds.find((item) => item.id === vpnClientMigrationTargets[client.id])
            const clientActionBusy = isActionResourceBusy(
              vpnPanelActionResourceKey(client.vpnPanelId),
              vpnInboundActionResourceKey(client.vpnInboundId),
              vpnClientActionResourceKey(client.id),
              ...(selectedMigrationInbound
                ? [vpnPanelActionResourceKey(selectedMigrationInbound.vpnPanelId), vpnInboundActionResourceKey(selectedMigrationInbound.id)]
                : [])
            )
            const clientNeedsReconciliation = client.syncStatus.includes('uncertain') || client.syncStatus.includes('compensation-failed')
            return (
              <div key={client.id} className="list-item-vertical">
                <div className="item-head">
                  <div>
                    <strong>{client.email}</strong>
                    <div className="muted">UUID {client.uuid} · inbound {inbound?.name ?? shortId(client.vpnInboundId)} · до {formatDate(client.expiryTime)}</div>
                    <div className="muted">Синхронизация: {client.syncStatus ? formatAdminDisplayLabel(client.syncStatus) : 'Неизвестно'} · {formatDate(client.lastSyncedAt)} · лимит устройств {client.limitIp ?? 0}</div>
                  </div>
                  <div className="item-status">
                    <StatusBadge value={client.enable ? 'Enabled' : 'Disabled'} />
                    {clientNeedsReconciliation && <StatusBadge value="SyncRequired" />}
                    {inbound && <StatusBadge value={inbound.protocol} />}
                  </div>
                </div>
                <div className="toolbar" hidden={!canWriteSection('panels')}>
                  {client.enable
                    ? <ConfirmButton className="button-secondary" disabled={clientActionBusy} message={`Отключить VPN-клиента "${client.email}"? Пользователь потеряет подключение.`} onConfirm={() => handleVpnClientAction(client, 'disable')}>Отключить</ConfirmButton>
                    : <PrimaryButton className="button-ghost" disabled={clientActionBusy} onClick={() => void handleVpnClientAction(client, 'enable')}>Включить</PrimaryButton>}
                  <PrimaryButton disabled={clientActionBusy} onClick={() => void handleVpnClientAction(client, 'sync')}>Синхронизировать</PrimaryButton>
                  <ConfirmButton disabled={clientActionBusy} message={`Необратимо обнулить счётчики трафика VPN-клиента "${client.email}" в 3x-ui? При сетевой неопределённости клиент будет помечен для ручной сверки.`} onConfirm={() => handleVpnClientAction(client, 'reset')}>Сбросить трафик</ConfirmButton>
                  {migrationOptionsCount > 0 && <>
                    <select aria-label={`Целевой inbound для ${client.email}`} value={vpnClientMigrationTargets[client.id] ?? ''} onChange={(e) => updateVpnClientMigrationTarget(client.id, e.target.value)}>
                      <option value="">Выберите inbound</option>
                      {migrationOptionGroups.map((group) => <optgroup key={group.panel.id} label={`${group.panel.name} · ${group.panel.region}`}>{group.inbounds.map((option) => <option key={option.id} value={option.id}>{option.name} · {option.protocol}:{option.port} · {option.usedCapacity}/{option.capacity}</option>)}</optgroup>)}
                    </select>
                    <ConfirmButton disabled={!vpnClientMigrationTargets[client.id] || clientActionBusy} message={`Перенести VPN-клиента "${client.email}"? Сначала будет занято по одному временному slot целевой панели, inbound и связанного VPN-сервера; после успешного удаления source-копии старые slots освободятся. При ошибке перенос будет отменён.`} onConfirm={() => handleMigrateVpnClient(client)}>Перенести</ConfirmButton>
                  </>}
                </div>
              </div>
            )
          })}{vpnClients.length === 0 && <EmptyState title="Клиентов нет" description="После выдачи VPN-доступов клиенты 3x-ui появятся здесь." />}{vpnHealthChecks.slice(0, 3).map((check) => <div key={check.id} className="list-item"><span>{check.version || 'неизвестно'} · {check.latencyMs ?? 0}ms · {check.errorMessage || 'ошибок нет'}</span><StatusBadge value={check.status} /></div>)}{vpnSyncRuns.slice(0, 3).map((run) => <div key={run.id} className="list-item"><span>{run.errorMessage || (run.summaryJson !== '{}' ? run.summaryJson : '') || shortId(run.id)}</span><StatusBadge value={run.status} /></div>)}</div>
          </>}
        </Card>
      </div>

      <div id="support" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('support')} hidden={activeSection !== 'support' || activeSectionLoadFailed}>
        <Card>
          <h3>Обращения в поддержку</h3>
          <div className="list-stack">{supportConversations.length === 0 && <EmptyState title="Нет обращений" description="Сообщения из кабинета и Telegram появятся в этом списке." />}{supportConversations.slice(0, 12).map((conversation) => {
            const statusBusy = actionBusyResourceKeys.has(supportActionResourceKey(conversation.id))
            return <div key={conversation.id} className={`list-item-vertical${selectedSupportConversationId === conversation.id ? ' selected-item' : ''}`}><div className="item-head"><div><strong>{conversation.subject || 'Обращение в поддержку'}</strong><div className="muted">{formatStatusLabel(conversation.channel)} · tg:{conversation.telegramUserId ?? '—'} · пользователь:{shortId(conversation.userId)}</div><div className="muted">Ответственный: {shortId(conversation.assignedToUserId)} · заметка: {conversation.internalNote || '—'}</div></div><StatusBadge value={conversation.status} /></div><div className="toolbar"><PrimaryButton className={selectedSupportConversationId === conversation.id ? 'button-secondary' : 'button-ghost'} onClick={() => selectSupportConversation(conversation.id)}>{selectedSupportConversationId === conversation.id ? 'Открыто' : 'Открыть'}</PrimaryButton>{canWriteSection('support') && <><PrimaryButton className="button-secondary" disabled={statusBusy} aria-busy={statusBusy} onClick={() => void handleSupportStatus('pending', conversation.id)}>В ожидание</PrimaryButton><PrimaryButton disabled={statusBusy} aria-busy={statusBusy} onClick={() => void handleSupportStatus(conversation.status === 'closed' ? 'open' : 'closed', conversation.id)} className="button-secondary">{conversation.status === 'closed' ? 'Переоткрыть' : 'Закрыть'}</PrimaryButton></>}</div></div>
          })}</div>
        </Card>
        <Card>
          <h3>Диалог поддержки</h3>
          <label><span>Обращение</span><select value={selectedSupportConversationId} onChange={(e) => selectSupportConversation(e.target.value)}><option value="">Не выбрано</option>{supportConversations.map((conversation) => <option key={conversation.id} value={conversation.id}>{conversation.subject || shortId(conversation.id)}</option>)}</select></label>
          {supportMessagesLoading && <LoadingBlock label="Загружаем сообщения поддержки..." />}
          {supportMessagesError && selectedSupportConversationId && (
            <div className="toast-error support-messages-error" role="alert">
              <p>Не удалось загрузить сообщения поддержки: {supportMessagesError}</p>
              <PrimaryButton type="button" className="button-secondary" disabled={supportMessagesLoading} aria-busy={supportMessagesLoading} onClick={() => void loadSupportMessages(selectedSupportConversationId, token, sessionOperationId.current)}>
                Повторить загрузку сообщений
              </PrimaryButton>
            </div>
          )}
          {!supportMessagesLoading && !supportMessagesError && selectedSupportConversationId && supportMessages.length === 0 && <EmptyState title="Сообщений нет" description="Для выбранного обращения сообщения пока не сохранены." />}
          <div className="list-stack mt-12">{supportMessages.slice(-12).map((message) => <div key={message.id} className="list-item-vertical"><div className="card-head"><strong>{formatStatusLabel(message.direction)}{message.isInternalNote ? ' · внутренняя заметка' : ''}</strong><span className="muted">{formatDate(message.createdAt)}</span></div><div>{message.text}</div></div>)}</div>
          <form hidden={!canWriteSection('support')} className="mt-12" aria-busy={actionBusyResourceKeys.has(supportActionResourceKey(selectedSupportConversationId))} onSubmit={(event) => { event.preventDefault(); void handleReplySupport() }}>
            <label><span>Ответ пользователю</span><textarea value={supportReplyText} onChange={(e) => setSupportReplyText(e.target.value)} rows={3} placeholder="Текст ответа" /></label>
            <PrimaryButton type="submit" disabled={!selectedSupportConversationId || !supportReplyText.trim() || actionBusyResourceKeys.has(supportActionResourceKey(selectedSupportConversationId))} aria-busy={actionBusyResourceKeys.has(supportActionResourceKey(selectedSupportConversationId))}>{supportConversations.find((conversation) => conversation.id === selectedSupportConversationId)?.telegramUserId ? 'Отправить через Telegram' : 'Сохранить ответ'}</PrimaryButton>
          </form>
          <form hidden={!canWriteSection('support')} className="mt-12" aria-busy={actionBusyResourceKeys.has(supportActionResourceKey(selectedSupportConversationId))} onSubmit={(event) => { event.preventDefault(); void handleSupportNote() }}>
            <label><span>Внутренняя заметка</span><textarea value={supportNoteText} onChange={(e) => setSupportNoteText(e.target.value)} rows={2} placeholder="Видно только администраторам" /></label>
            <PrimaryButton type="submit" disabled={!selectedSupportConversationId || !supportNoteText.trim() || actionBusyResourceKeys.has(supportActionResourceKey(selectedSupportConversationId))} aria-busy={actionBusyResourceKeys.has(supportActionResourceKey(selectedSupportConversationId))} className="button-secondary">Добавить заметку</PrimaryButton>
          </form>
        </Card>
      </div>

      <div id="bot" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('bot')} hidden={activeSection !== 'bot' || activeSectionLoadFailed}>
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
          <form aria-busy={botSettingsActionBusy} onSubmit={(event) => { event.preventDefault(); void handleSaveBotSettings() }}>
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
            <FormValidationSummary errors={botSettingsFormErrors} />
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={!token || !botSettingsFormChanged || botSettingsActionBusy || botSettingsFormErrors.length > 0} title={adminDisabledTitle} aria-busy={botSettingsActionBusy}>Сохранить настройки бота</PrimaryButton>
              <PrimaryButton type="button" className="button-ghost" disabled={!botSettingsFormChanged || botSettingsActionBusy} onClick={() => { botSettingsFormDirty.current = false; setBotSettingsForm(telegramBotSettingsToForm(botSettings)) }}>Отменить изменения</PrimaryButton>
              <PrimaryButton className="button-secondary" type="button" disabled={!token || botSettingsActionBusy} title={adminDisabledTitle} aria-busy={botSettingsActionBusy} onClick={() => { void handleTestBotSettings() }}>Проверить подключение</PrimaryButton>
            </div>
          </form>
        </Card>
      </div>

      <div id="releases" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('releases')} hidden={activeSection !== 'releases' || activeSectionLoadFailed}>
        <Card>
          <h3>{editingReleaseId ? 'Редактировать релиз' : 'Создать релиз'}</h3>
          <p className="muted">Эти записи показываются пользователям в окне «Что нового» после входа в личный кабинет. Будущие даты публикации не показываются до наступления времени.</p>
          <form hidden={!canWriteSection('releases')} aria-busy={releaseFormActionBusy} onSubmit={(event) => { event.preventDefault(); void handleSaveRelease() }}>
            <fieldset className="form-section">
              <legend>Публикация</legend>
              <div className="form-grid">
                <label><span>Release ID</span><input value={releaseForm.releaseId} onChange={(e) => updateReleaseForm('releaseId', e.target.value)} placeholder="2026-05-27-whats-new-module" maxLength={160} pattern="[a-z0-9]+(?:-[a-z0-9]+)*" required /></label>
                <label><span>Версия</span><input value={releaseForm.version} onChange={(e) => updateReleaseForm('version', e.target.value)} placeholder="0.2.0" maxLength={40} required /></label>
                <label><span>Дата публикации</span><input value={toDateTimeLocalValue(releaseForm.releasedAt)} onChange={(e) => updateReleaseForm('releasedAt', fromDateTimeLocalValue(e.target.value))} type="datetime-local" required /></label>
                <label><span>Источник</span><select value={releaseForm.source ?? 'manual'} onChange={(e) => updateReleaseForm('source', e.target.value)}><option value="manual">Вручную</option><option value="agent">Агент</option></select></label>
              </div>
              <label className="checkbox-row"><input checked={releaseForm.isActive} onChange={(e) => updateReleaseForm('isActive', e.target.checked)} type="checkbox" /> Опубликован и виден пользователям</label>
            </fieldset>
            <fieldset className="form-section">
              <legend>Описание для пользователей</legend>
              <label><span>Заголовок</span><input value={releaseForm.title} onChange={(e) => updateReleaseForm('title', e.target.value)} placeholder="Что изменилось" maxLength={200} required /></label>
              <label><span>Короткое описание</span><textarea value={releaseForm.summary} onChange={(e) => updateReleaseForm('summary', e.target.value)} rows={3} placeholder="Коротко объясните, где пользователь увидит изменения" maxLength={4000} required /></label>
            </fieldset>
            <fieldset className="form-section">
              <legend>Пункты релиза</legend>
              <div className="list-stack">
                {releaseForm.items.map((item, index) => (
                  <div key={index} className="release-item-editor">
                    <label><span>Тип</span><select value={item.type} onChange={(e) => updateReleaseItem(index, { type: e.target.value })}><option value="new">Новое</option><option value="improved">Улучшено</option><option value="fixed">Исправлено</option><option value="important">Важно</option></select></label>
                    <label><span>Порядок</span><input value={item.sortOrder} onChange={(e) => updateReleaseItem(index, { sortOrder: Number(e.target.value) || 0 })} type="number" min={0} step="1" /></label>
                    <label className="release-item-text"><span>Текст</span><textarea value={item.text} onChange={(e) => updateReleaseItem(index, { text: e.target.value })} rows={2} placeholder="Пишите для пользователя, без названий файлов и коммитов" maxLength={4000} required /></label>
                    <PrimaryButton type="button" className="button-ghost" disabled={releaseForm.items.length <= 1} onClick={() => removeReleaseItem(index)}>Убрать</PrimaryButton>
                  </div>
                ))}
              </div>
              <PrimaryButton type="button" className="button-secondary mt-12" disabled={releaseForm.items.length >= 100} onClick={addReleaseItem}>Добавить пункт</PrimaryButton>
            </fieldset>
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={!token || !releaseFormChanged || releaseFormActionBusy || !releaseForm.releaseId || !releaseForm.title || !releaseForm.summary} title={adminDisabledTitle} aria-busy={releaseFormActionBusy}>
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
                  {release.items.map((item, index) => <div key={`${release.id}-${index}`} className="list-item"><span>{formatStatusLabel(item.type)}: {item.text}</span></div>)}
                </div>
                <div className="toolbar" hidden={!canWriteSection('releases')}>
                  <PrimaryButton className="button-secondary" disabled={isActionResourceBusy(appReleaseActionResourceKey(release.id))} onClick={() => editRelease(release)}>Редактировать</PrimaryButton>
                  <ConfirmButton className="button-danger" disabled={isActionResourceBusy(appReleaseActionResourceKey(release.id))} message={`Удалить релиз "${release.title}"? Пользователи больше не увидят его в истории.`} onConfirm={() => handleDeleteRelease(release)}>Удалить</ConfirmButton>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>

      <div id="faq" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('faq')} hidden={activeSection !== 'faq' || activeSectionLoadFailed}>
        <Card>
          <h3>{editingFaqId ? 'Редактировать вопрос' : 'Создать вопрос FAQ'}</h3>
          <p className="muted">Эти вопросы показываются на публичной странице FAQ. Неактивные записи остаются в админке, но скрываются от пользователей.</p>
          <form hidden={!canWriteSection('faq')} aria-busy={faqFormActionBusy} onSubmit={(event) => { event.preventDefault(); void handleSaveFaq() }}>
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
              <PrimaryButton type="submit" disabled={!token || faqFormActionBusy || !faqForm.question || !faqForm.answer || !faqFormChanged} title={adminDisabledTitle} aria-busy={faqFormActionBusy}>
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
                  <PrimaryButton className="button-secondary" disabled={Boolean(entry.id && isActionResourceBusy(faqActionResourceKey(entry.id)))} onClick={() => editFaq(entry)}>Редактировать</PrimaryButton>
                  <ConfirmButton className="button-danger" disabled={Boolean(entry.id && isActionResourceBusy(faqActionResourceKey(entry.id)))} message={`Удалить вопрос "${entry.question}"?`} onConfirm={() => handleDeleteFaq(entry)}>Удалить</ConfirmButton>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>

      <div id="content" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('content')} hidden={activeSection !== 'content' || activeSectionLoadFailed}>
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
                disabled={!token || siteContentActionBusy}
                message="Восстановить обязательные блоки главной? Недостающие блоки будут созданы, пустые или выключенные обязательные блоки получат безопасные значения по умолчанию."
                onConfirm={() => handleRestoreHomeContentDefaults()}
              >
                Восстановить главную
              </ConfirmButton>
            </div>
          </div>
          <form hidden={!canWriteSection('content')} aria-busy={siteContentActionBusy} onSubmit={(event) => { event.preventDefault(); void handleSaveSiteContent() }}>
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
              <PrimaryButton type="submit" disabled={!token || siteContentActionBusy || !siteContentForm.key || !siteContentFormChanged} title={adminDisabledTitle} aria-busy={siteContentActionBusy}>{editingSiteContentId ? 'Сохранить блок' : 'Создать блок'}</PrimaryButton>
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
                  <PrimaryButton className="button-secondary" disabled={siteContentActionBusy} onClick={() => editSiteContent(block)}>Редактировать</PrimaryButton>
                  <ConfirmButton className="button-danger" disabled={siteContentActionBusy} message={`Удалить блок "${block.label}"? На сайте будет использован fallback-текст из приложения.`} onConfirm={() => handleDeleteSiteContent(block)}>Удалить</ConfirmButton>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>

      <div id="scenarios" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('scenarios')} hidden={activeSection !== 'scenarios' || activeSectionLoadFailed}>
        <Card>
          <h3>{editingWorkScenarioId ? 'Редактировать сценарий' : 'Создать сценарий работы'}</h3>
          <p className="muted">Сценарий описывает выдачу VPN после оплаты, поведение при ошибке, возврате, продлении и окончании подписки. Тариф выбирает сценарий по ключу.</p>
          <form hidden={!canWriteSection('scenarios')} aria-busy={workScenarioFormActionBusy} onSubmit={(event) => { event.preventDefault(); void handleSaveWorkScenario() }}>
            <fieldset className="form-section">
              <legend>Основные параметры</legend>
              <div className="form-grid">
                <label><span>Название</span><input value={workScenarioForm.name} onChange={(e) => updateWorkScenarioForm('name', e.target.value)} placeholder="Автоматическая выдача VPN" maxLength={200} required /></label>
                <label><span>Ключ</span><input value={workScenarioForm.key} onChange={(e) => updateWorkScenarioForm('key', e.target.value)} placeholder="auto" maxLength={120} required /></label>
                <label><span>VPN-протокол</span><select value={workScenarioForm.vpnProtocol} onChange={(e) => updateWorkScenarioForm('vpnProtocol', e.target.value)}><option value="vless">VLESS</option><option value="vmess">VMess</option><option value="trojan">Trojan</option></select></label>
                <label><span>Режим выдачи</span><select value={workScenarioForm.provisioningMode} onChange={(e) => updateWorkScenarioForm('provisioningMode', e.target.value)}><option value="auto">Автоматически</option><option value="manual">Вручную</option><option value="hybrid">Гибридно</option></select></label>
                <label><span>Правило сервера</span><input value={workScenarioForm.serverSelectionRule} onChange={(e) => updateWorkScenarioForm('serverSelectionRule', e.target.value)} placeholder="least-loaded" maxLength={120} /></label>
                <label><span>Правило inbound</span><input value={workScenarioForm.inboundSelectionRule} onChange={(e) => updateWorkScenarioForm('inboundSelectionRule', e.target.value)} placeholder="default" maxLength={120} /></label>
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
              <label><span>После успешной оплаты</span><textarea value={workScenarioForm.onPaymentSucceeded} onChange={(e) => updateWorkScenarioForm('onPaymentSucceeded', e.target.value)} rows={2} maxLength={4000} /></label>
              <label><span>После ошибки оплаты</span><textarea value={workScenarioForm.onPaymentFailed} onChange={(e) => updateWorkScenarioForm('onPaymentFailed', e.target.value)} rows={2} maxLength={4000} /></label>
              <label><span>После возврата</span><textarea value={workScenarioForm.onRefund} onChange={(e) => updateWorkScenarioForm('onRefund', e.target.value)} rows={2} maxLength={4000} /></label>
              <label><span>После окончания подписки</span><textarea value={workScenarioForm.onSubscriptionExpired} onChange={(e) => updateWorkScenarioForm('onSubscriptionExpired', e.target.value)} rows={2} maxLength={4000} /></label>
              <label><span>После продления</span><textarea value={workScenarioForm.onRenewal} onChange={(e) => updateWorkScenarioForm('onRenewal', e.target.value)} rows={2} maxLength={4000} /></label>
            </fieldset>
            <fieldset className="form-section">
              <legend>Тексты для пользователя</legend>
              <label><span>Текст для кабинета</span><textarea value={workScenarioForm.cabinetText} onChange={(e) => updateWorkScenarioForm('cabinetText', e.target.value)} rows={3} maxLength={4000} /></label>
              <label><span>Текст для Telegram</span><textarea value={workScenarioForm.telegramText} onChange={(e) => updateWorkScenarioForm('telegramText', e.target.value)} rows={3} maxLength={4000} /></label>
            </fieldset>
            <FormValidationSummary errors={workScenarioForm !== defaultWorkScenarioForm ? workScenarioFormErrors : []} />
            <div className="form-footer">
              <PrimaryButton type="submit" disabled={!token || workScenarioFormActionBusy || workScenarioFormErrors.length > 0 || !workScenarioFormChanged} title={adminDisabledTitle} aria-busy={workScenarioFormActionBusy}>{editingWorkScenarioId ? 'Сохранить сценарий' : 'Создать сценарий'}</PrimaryButton>
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
                    <div className="muted">{scenario.key} · {scenario.vpnProtocol} · {provisioningModeLabel(scenario.provisioningMode)} · сервер {formatAdminDisplayLabel(scenario.serverSelectionRule)} · inbound {formatAdminDisplayLabel(scenario.inboundSelectionRule)}</div>
                    <div className="muted">Оплата: {formatAdminDisplayLabel(scenario.onPaymentSucceeded)} · Ошибка оплаты: {formatAdminDisplayLabel(scenario.onPaymentFailed)}</div>
                    <div className="muted">Возврат: {formatAdminDisplayLabel(scenario.onRefund)} · Окончание: {formatAdminDisplayLabel(scenario.onSubscriptionExpired)} · Продление: {formatAdminDisplayLabel(scenario.onRenewal)}</div>
                    <div className="muted">Тарифы: {tariffs.filter((tariff) => tariff.provisioningScenario === scenario.key).map((tariff) => tariff.name).join(', ') || 'не выбраны'}</div>
                  </div>
                  <div className="item-status"><StatusBadge value={scenario.isActive ? 'Active' : 'Hidden'} /><StatusBadge value={formatAdminDisplayLabel(scenario.generateQrCode ? 'QrEnabled' : 'QrDisabled')} /></div>
                </div>
                <div className="toolbar" hidden={!canWriteSection('scenarios')}>
                  <PrimaryButton className="button-secondary" disabled={isActionResourceBusy(workScenarioActionResourceKey(scenario.id))} onClick={() => editWorkScenario(scenario)}>Редактировать</PrimaryButton>
                  <ConfirmButton className="button-danger" disabled={isActionResourceBusy(workScenarioActionResourceKey(scenario.id))} message={`Удалить сценарий "${scenario.name}"? Если он выбран в тарифе, API не даст удалить его.`} onConfirm={() => handleDeleteWorkScenario(scenario)}>Удалить</ConfirmButton>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>

      <div id="provisioning" className="section card-list-two" role="tabpanel" aria-labelledby={adminSectionTabId('provisioning')} hidden={activeSection !== 'provisioning' || activeSectionLoadFailed}>
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
                <div className="muted">Запуск: {shortId(run.id)} · источник {formatAdminDisplayLabel(run.source)} · владелец {formatAdminDisplayLabel(run.owner)} · шаг {formatAdminDisplayLabel(run.currentStep || run.status)}</div>
                <div className="muted">Цель: {run.targetHost || shortId(run.nodeId)}:{run.sshPort ?? 22} · пользователь {run.username || 'root'} · авторизация {formatAdminDisplayLabel(run.authMethod)} · доступы {run.credentialsConfigured ? 'заданы' : 'не заданы'} · {run.validationMode ? 'проверочный сервер' : 'рабочий кандидат'}</div>
                <div className="muted">Режим запуска: {provisioningDeployModeLabel(run.mode)} · риск {provisioningRiskLabel(run.riskLevel)} · рабочее развёртывание {run.liveDeployAllowed ? 'разрешено' : 'закрыто'} · {run.nextAction || 'проверьте результат перед следующим действием'}</div>
                <div className="muted">Следующее развёртывание: {provisioningDeployModeLabel(run.deployMode)} · риск {provisioningRiskLabel(run.deployRiskLevel)} · {run.deployNextAction || 'сначала выполните предварительную проверку'}</div>
                <div className="muted">{run.dryRun ? 'проверка без изменений' : 'развертывание'} · старт {formatDate(run.startedAt)} · финиш {formatDate(run.finishedAt)}</div>
                {(run.attemptCount ?? 0) > 0 && <div className="muted">Попытка {run.attemptCount} · обработка {formatDate(run.processingStartedAt)} · аренда задачи до {formatDate(run.leaseExpiresAt)}</div>}
                {run.operatorWarning && <div className="safe-note">{run.operatorWarning}</div>}
                {run.precheckReportPreview && <pre className="safe-note">{run.precheckReportPreview}</pre>}
                <div className="muted">{run.lastError || run.errorSummary || run.executionLogPreview || run.executionLog || '—'}</div>
                <div className="toolbar" hidden={!canWriteSection('provisioning')}>
                  <PrimaryButton disabled={!token || isActionResourceBusy(provisioningRunActionResourceKey(run.id), serverActionResourceKey(run.nodeId)) || !canRetryProvisioningRun(run.status)} onClick={() => void handleRetryProvisioningRun(run)}>Повторить</PrimaryButton>
                  <ConfirmButton disabled={!token || isActionResourceBusy(provisioningRunActionResourceKey(run.id), serverActionResourceKey(run.nodeId)) || !['ReadyToDeploy', 'Succeeded'].includes(run.status) || run.deployMode === 'live-deploy-blocked'} className="button-danger" message={`Развернуть VPS? Режим: ${provisioningDeployModeLabel(run.deployMode)}. ${run.deployOperatorWarning || run.operatorWarning || 'В рабочем режиме это может выполнить реальные SSH/Ansible-действия.'}`} onConfirm={() => handleDeployProvisioningRun(run)}>Развернуть</ConfirmButton>
                  <ConfirmButton disabled={!token || isActionResourceBusy(provisioningRunActionResourceKey(run.id), serverActionResourceKey(run.nodeId)) || !canCancelProvisioningRun(run.status)} className="button-secondary" message="Отменить запуск подготовки VPS?" onConfirm={() => handleCancelProvisioningRun(run)}>Отменить</ConfirmButton>
                  <PrimaryButton disabled={!token || isActionResourceBusy(provisioningRunActionResourceKey(run.id), serverActionResourceKey(run.nodeId))} onClick={() => void handleProvisioningSupportNeeded(run)}>Нужна поддержка</PrimaryButton>
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
