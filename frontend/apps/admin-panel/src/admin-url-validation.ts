import { getSafeHttpUrl } from '@vpn-platform/ui'

type TelegramBotUrlFields = {
  webhookUrl?: string | null
  webAppUrl?: string | null
}

export function isOptionalSafeAdminHttpUrl(value?: string | null) {
  const normalized = String(value ?? '').trim()
  return !normalized || getSafeHttpUrl(normalized) !== null
}

export function validateTelegramBotUrlFields(form: TelegramBotUrlFields) {
  const errors: string[] = []
  if (!isOptionalSafeAdminHttpUrl(form.webhookUrl)) errors.push('Webhook URL должен быть корректным http/https адресом без логина и пароля.')
  if (!isOptionalSafeAdminHttpUrl(form.webAppUrl)) errors.push('WebApp URL должен быть корректным http/https адресом без логина и пароля.')
  return errors
}
