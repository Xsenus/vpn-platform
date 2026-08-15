import type {
  AdminFaqItem,
  FaqUpsertPayload,
  SiteContentBlockDto,
  SiteContentBlockUpsertPayload,
  WorkScenarioDto,
  WorkScenarioUpsertPayload
} from '@vpn-platform/api-client'

export function normalizeFaqPayload(form: FaqUpsertPayload): FaqUpsertPayload {
  return {
    question: form.question.trim(),
    answer: form.answer.trim(),
    category: form.category?.trim() || 'Общее',
    isActive: form.isActive,
    showOnHome: form.showOnHome,
    showOnFaqPage: form.showOnFaqPage,
    sortOrder: Number(form.sortOrder) || 0
  }
}

export function isFaqFormChanged(form: FaqUpsertPayload, current: AdminFaqItem): boolean {
  const candidate = normalizeFaqPayload(form)
  return candidate.question !== current.question
    || candidate.answer !== current.answer
    || candidate.category !== current.category
    || candidate.isActive !== current.isActive
    || candidate.showOnHome !== current.showOnHome
    || candidate.showOnFaqPage !== current.showOnFaqPage
    || candidate.sortOrder !== current.sortOrder
}

export function normalizeSiteContentPayload(form: SiteContentBlockUpsertPayload): SiteContentBlockUpsertPayload {
  const key = form.key.trim()
  return {
    key,
    value: form.value ?? '',
    group: form.group?.trim() || 'home',
    label: form.label?.trim() || key,
    description: form.description?.trim() || '',
    inputType: form.inputType?.trim() || 'text',
    isActive: form.isActive,
    sortOrder: Number(form.sortOrder) || 0
  }
}

export function isSiteContentFormChanged(form: SiteContentBlockUpsertPayload, current: SiteContentBlockDto): boolean {
  const candidate = normalizeSiteContentPayload(form)
  return candidate.key !== current.key
    || candidate.value !== current.value
    || candidate.group !== current.group
    || candidate.label !== current.label
    || candidate.description !== current.description
    || candidate.inputType !== current.inputType
    || candidate.isActive !== current.isActive
    || candidate.sortOrder !== current.sortOrder
}

export function parseWorkScenarioTariffIds(value?: string | null): string[] {
  try {
    const parsed = JSON.parse(value || '[]')
    return Array.isArray(parsed)
      ? parsed.filter((item): item is string => typeof item === 'string' && item.trim().length > 0)
      : []
  } catch {
    return []
  }
}

export function scenarioTariffIdsToJson(ids: string[]): string {
  return JSON.stringify(Array.from(new Set(ids.filter(Boolean))))
}

function normalizedText(value: string, fallback: string): string {
  return value.trim() || fallback
}

function normalizeScenarioKey(value: string): string {
  const key = value.trim().toLowerCase()
    .replace(/\s+/g, '-')
    .replace(/[^a-z0-9\-_]+/g, '-')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '')
  return key || 'scenario'
}

export function normalizeWorkScenarioPayload(form: WorkScenarioUpsertPayload): WorkScenarioUpsertPayload {
  return {
    name: form.name.trim(),
    key: normalizeScenarioKey(form.key),
    isActive: form.isActive,
    allowedTariffIdsJson: scenarioTariffIdsToJson(parseWorkScenarioTariffIds(form.allowedTariffIdsJson)),
    vpnProtocol: normalizedText(form.vpnProtocol, 'vless').toLowerCase(),
    serverSelectionRule: normalizedText(form.serverSelectionRule, 'least-loaded'),
    inboundSelectionRule: normalizedText(form.inboundSelectionRule, 'default'),
    provisioningMode: normalizedText(form.provisioningMode, 'auto').toLowerCase(),
    onPaymentSucceeded: normalizedText(form.onPaymentSucceeded, 'create_subscription_and_access'),
    onPaymentFailed: normalizedText(form.onPaymentFailed, 'keep_order_pending'),
    onRefund: normalizedText(form.onRefund, 'disable_access'),
    onSubscriptionExpired: normalizedText(form.onSubscriptionExpired, 'disable_access_after_grace'),
    onRenewal: normalizedText(form.onRenewal, 'extend_subscription'),
    cabinetText: form.cabinetText.trim(),
    telegramText: form.telegramText.trim(),
    generateQrCode: form.generateQrCode,
    maxDevices: Number(form.maxDevices) || 1,
    trafficLimit: form.trafficLimit ?? null,
    sortOrder: Number(form.sortOrder) || 0
  }
}

export function isWorkScenarioFormChanged(form: WorkScenarioUpsertPayload, current: WorkScenarioDto): boolean {
  const candidate = normalizeWorkScenarioPayload(form)
  return candidate.name !== current.name
    || candidate.key !== current.key
    || candidate.isActive !== current.isActive
    || candidate.allowedTariffIdsJson !== current.allowedTariffIdsJson
    || candidate.vpnProtocol !== current.vpnProtocol
    || candidate.serverSelectionRule !== current.serverSelectionRule
    || candidate.inboundSelectionRule !== current.inboundSelectionRule
    || candidate.provisioningMode !== current.provisioningMode
    || candidate.onPaymentSucceeded !== current.onPaymentSucceeded
    || candidate.onPaymentFailed !== current.onPaymentFailed
    || candidate.onRefund !== current.onRefund
    || candidate.onSubscriptionExpired !== current.onSubscriptionExpired
    || candidate.onRenewal !== current.onRenewal
    || candidate.cabinetText !== current.cabinetText
    || candidate.telegramText !== current.telegramText
    || candidate.generateQrCode !== current.generateQrCode
    || candidate.maxDevices !== current.maxDevices
    || candidate.trafficLimit !== (current.trafficLimit ?? null)
    || candidate.sortOrder !== current.sortOrder
}
