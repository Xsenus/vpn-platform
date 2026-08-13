import type { WorkScenarioUpsertPayload } from '@vpn-platform/api-client'

export function validateWorkScenarioForm(form: WorkScenarioUpsertPayload) {
  const errors: string[] = []
  const key = form.key.trim()

  if (!form.name.trim()) errors.push('Укажите название сценария.')
  if (form.name.trim().length > 200) errors.push('Название сценария не должно превышать 200 символов.')
  if (!key) errors.push('Укажите ключ сценария.')
  if (key.length > 120) errors.push('Ключ сценария не должен превышать 120 символов.')
  if (key && !/^[a-z0-9_-]+(?:-[a-z0-9_-]+)*$/i.test(key)) errors.push('Ключ может содержать латинские буквы, цифры, дефис и подчёркивание.')
  if (form.allowedTariffIdsJson.length > 4000) errors.push('Выбрано слишком много тарифов для одного сценария.')
  if (form.serverSelectionRule.trim().length > 120) errors.push('Правило выбора сервера не должно превышать 120 символов.')
  if (form.inboundSelectionRule.trim().length > 120) errors.push('Правило выбора inbound не должно превышать 120 символов.')
  if ([form.onPaymentSucceeded, form.onPaymentFailed, form.onRefund, form.onSubscriptionExpired, form.onRenewal, form.cabinetText, form.telegramText].some((value) => value.trim().length > 4000)) {
    errors.push('Текстовые поля сценария не должны превышать 4000 символов.')
  }
  if (Number(form.maxDevices) <= 0) errors.push('Количество устройств должно быть больше 0.')
  if (!['auto', 'manual', 'hybrid'].includes(String(form.provisioningMode || '').trim().toLowerCase())) errors.push('Режим выдачи должен быть одним из вариантов: автоматически, вручную или гибридно.')

  return errors
}
