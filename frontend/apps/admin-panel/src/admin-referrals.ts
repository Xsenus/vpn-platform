import type { AdminReferralProgramDto, ReferralProgramUpsertPayload } from '@vpn-platform/api-client'

export type ReferralProgramFormState = {
  revision: number
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
  sourceRuleDefinition: string
  sourceRewardDefinition: string
  antiFraudSettings: string
}

export const defaultReferralProgramForm: ReferralProgramFormState = {
  revision: 0,
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
  referredAutoApprove: true,
  sourceRuleDefinition: '{}',
  sourceRewardDefinition: '{}',
  antiFraudSettings: '{}'
}

function parseJsonObject(value: string): Record<string, unknown> {
  try {
    const parsed = JSON.parse(value)
    return typeof parsed === 'object' && parsed !== null && !Array.isArray(parsed) ? parsed as Record<string, unknown> : {}
  } catch {
    return {}
  }
}

function toDateTimeLocalValue(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const offset = date.getTimezoneOffset()
  return new Date(date.getTime() - offset * 60_000).toISOString().slice(0, 16)
}

export function referralProgramToForm(program: AdminReferralProgramDto): ReferralProgramFormState {
  const rules = parseJsonObject(program.ruleDefinition)
  const rewards = parseJsonObject(program.rewardDefinition)
  const referrer = typeof rewards.referrer === 'object' && rewards.referrer ? rewards.referrer as Record<string, unknown> : null
  const referred = typeof rewards.referred === 'object' && rewards.referred ? rewards.referred as Record<string, unknown> : null
  return {
    ...defaultReferralProgramForm,
    revision: program.revision,
    name: program.name,
    status: program.status as ReferralProgramFormState['status'],
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
    referredAutoApprove: referred?.autoApprove === true,
    sourceRuleDefinition: program.ruleDefinition,
    sourceRewardDefinition: program.rewardDefinition,
    antiFraudSettings: program.antiFraudSettings
  }
}

function buildReward(current: unknown, type: string, value: number, unit: string, autoApprove: boolean) {
  const extension = typeof current === 'object' && current !== null && !Array.isArray(current) ? current as Record<string, unknown> : {}
  return { ...extension, type: type.trim(), value, unit: unit.trim(), autoApprove }
}

export function buildReferralProgramPayload(form: ReferralProgramFormState): ReferralProgramUpsertPayload {
  const rules = parseJsonObject(form.sourceRuleDefinition)
  rules.firstPurchaseOnly = form.firstPurchaseOnly
  rules.minimumOrderAmount = form.minimumOrderAmount
  rules.allowedChannels = form.allowedChannels

  const rewards = parseJsonObject(form.sourceRewardDefinition)
  if (form.referrerEnabled) rewards.referrer = buildReward(rewards.referrer, form.referrerType, form.referrerValue, form.referrerUnit, form.referrerAutoApprove)
  else delete rewards.referrer
  if (form.referredEnabled) rewards.referred = buildReward(rewards.referred, form.referredType, form.referredValue, form.referredUnit, form.referredAutoApprove)
  else delete rewards.referred

  return {
    name: form.name.trim(),
    status: form.status,
    startAt: form.startAt ? new Date(form.startAt).toISOString() : null,
    endAt: form.endAt ? new Date(form.endAt).toISOString() : null,
    ruleDefinition: JSON.stringify(rules),
    rewardDefinition: JSON.stringify(rewards),
    antiFraudSettings: form.antiFraudSettings.trim()
  }
}

export function isReferralProgramFormChanged(form: ReferralProgramFormState, current: AdminReferralProgramDto): boolean {
  const candidate = buildReferralProgramPayload(form)
  const sameDate = (left?: string | null, right?: string | null) => (!left || !right)
    ? !left && !right
    : new Date(left).getTime() === new Date(right).getTime()
  return candidate.name !== current.name
    || candidate.status !== current.status
    || !sameDate(candidate.startAt, current.startAt)
    || !sameDate(candidate.endAt, current.endAt)
    || candidate.ruleDefinition !== current.ruleDefinition
    || candidate.rewardDefinition !== current.rewardDefinition
    || candidate.antiFraudSettings !== current.antiFraudSettings
}

export function validateReferralProgramForm(form: ReferralProgramFormState) {
  const errors: string[] = []
  if (!form.name.trim()) errors.push('Укажите название программы.')
  else if (form.name.trim().length > 160) errors.push('Название программы не должно превышать 160 символов.')
  if (!Number.isFinite(form.minimumOrderAmount) || form.minimumOrderAmount < 0) errors.push('Минимальная сумма заказа должна быть неотрицательным числом.')
  if (!form.referrerEnabled && !form.referredEnabled) errors.push('Выберите хотя бы одного получателя вознаграждения.')
  validateReward(form.referrerEnabled, form.referrerType, form.referrerValue, form.referrerUnit, 'пригласившему пользователю', errors)
  validateReward(form.referredEnabled, form.referredType, form.referredValue, form.referredUnit, 'приглашенному пользователю', errors)
  if (form.startAt && form.endAt && new Date(form.endAt) <= new Date(form.startAt)) errors.push('Дата окончания должна быть позже даты начала.')
  return errors
}

function validateReward(enabled: boolean, type: string, value: number, unit: string, recipient: string, errors: string[]) {
  if (!enabled) return
  if (!Number.isFinite(value) || value <= 0 || !type.trim() || !unit.trim()) errors.push(`Заполните вознаграждение ${recipient}.`)
  else if (value > 1_000_000) errors.push(`Значение вознаграждения ${recipient} не должно превышать 1 000 000.`)
  if (type.trim().length > 64) errors.push(`Тип вознаграждения ${recipient} не должен превышать 64 символа.`)
  if (unit.trim().length > 32) errors.push(`Единица вознаграждения ${recipient} не должна превышать 32 символа.`)
}
