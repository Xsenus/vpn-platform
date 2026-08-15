import type { PaymentProviderAccountDto, UpsertPaymentProviderAccountPayload } from '@vpn-platform/api-client'

const normalize = (value?: string | null) => value?.trim() ?? ''
const normalizeUrl = (value?: string | null) => normalize(value).replace(/\/+$/, '')

export function isPaymentProviderAccountFormChanged(
  form: UpsertPaymentProviderAccountPayload,
  current: PaymentProviderAccountDto,
  defaultApiBaseUrl: string
): boolean {
  const name = normalize(form.name) || form.provider
  const publicName = normalize(form.publicName) || name
  const apiBaseUrl = normalizeUrl(form.apiBaseUrl) || normalizeUrl(defaultApiBaseUrl)
  const extraSettingsJson = normalize(form.extraSettingsJson) || current.extraSettingsJson

  return Boolean(normalize(form.secretKey) || normalize(form.webhookSecret))
    || form.provider !== current.provider
    || form.mode !== current.mode
    || name !== current.name
    || publicName !== current.publicName
    || form.isEnabled !== current.isEnabled
    || form.isDefault !== current.isDefault
    || normalize(form.shopId) !== current.shopId
    || apiBaseUrl !== current.apiBaseUrl
    || normalize(form.returnUrl) !== current.returnUrl
    || normalize(form.webhookUrl) !== current.webhookUrl
    || form.useWebhookIpAllowList !== current.useWebhookIpAllowList
    || normalize(form.allowedWebhookIpRangesCsv) !== current.allowedWebhookIpRangesCsv
    || extraSettingsJson !== current.extraSettingsJson
}
