import type { AdminTelegramBotSettingsDto, UpdateTelegramBotSettingsPayload } from '@vpn-platform/api-client'

function normalizeText(value: string | null | undefined) {
  return (value ?? '').trim()
}

function normalizeUsername(value: string | null | undefined) {
  return normalizeText(value).replace(/^@+/, '')
}

function normalizeMode(value: string | null | undefined) {
  return normalizeText(value).toLowerCase() === 'webhook' ? 'Webhook' : 'LongPolling'
}

export function telegramBotSettingsToForm(settings: AdminTelegramBotSettingsDto): UpdateTelegramBotSettingsPayload {
  return {
    enabled: settings.enabled,
    mode: settings.mode,
    publicBotUsername: settings.publicBotUsername,
    botToken: '',
    webhookUrl: settings.webhookUrl,
    secretToken: '',
    adminChatId: settings.adminChatId,
    webAppUrl: settings.webAppUrl,
    welcomeText: settings.welcomeText,
    instructionText: settings.instructionText,
    supportText: settings.supportText,
    afterPaymentTextTemplate: settings.afterPaymentTextTemplate,
    renewalTextTemplate: settings.renewalTextTemplate,
    paymentFailedTextTemplate: settings.paymentFailedTextTemplate,
    subscriptionExpiredTextTemplate: settings.subscriptionExpiredTextTemplate
  }
}

export function isTelegramBotSettingsFormChanged(
  form: UpdateTelegramBotSettingsPayload,
  settings: AdminTelegramBotSettingsDto
) {
  return Boolean(normalizeText(form.botToken) || normalizeText(form.secretToken))
    || Boolean(form.enabled) !== settings.enabled
    || normalizeMode(form.mode) !== settings.mode
    || normalizeUsername(form.publicBotUsername) !== settings.publicBotUsername
    || normalizeText(form.webhookUrl) !== settings.webhookUrl
    || normalizeText(form.adminChatId) !== settings.adminChatId
    || normalizeText(form.webAppUrl) !== settings.webAppUrl
    || normalizeText(form.welcomeText) !== settings.welcomeText
    || normalizeText(form.instructionText) !== settings.instructionText
    || normalizeText(form.supportText) !== settings.supportText
    || normalizeText(form.afterPaymentTextTemplate) !== settings.afterPaymentTextTemplate
    || normalizeText(form.renewalTextTemplate) !== settings.renewalTextTemplate
    || normalizeText(form.paymentFailedTextTemplate) !== settings.paymentFailedTextTemplate
    || normalizeText(form.subscriptionExpiredTextTemplate) !== settings.subscriptionExpiredTextTemplate
}
