import test from 'node:test'
import assert from 'node:assert/strict'
import {
  buildReferralProgramPayload,
  isReferralProgramFormChanged,
  referralProgramToForm,
  validateReferralProgramForm
} from '../apps/admin-panel/src/admin-referrals.ts'

test('referral program edit preserves opaque configuration extensions', () => {
  const program = {
    id: 'program-1',
    revision: 3,
    name: 'Welcome',
    status: 'active',
    startAt: null,
    endAt: null,
    ruleDefinition: '{"firstPurchaseOnly":true,"minimumOrderAmount":100,"allowedChannels":["Web"],"campaign":"summer"}',
    rewardDefinition: '{"referrer":{"type":"bonus-days","value":7,"unit":"days","autoApprove":true,"tier":"gold"},"partner":{"type":"cashback","value":1,"unit":"RUB"}}',
    antiFraudSettings: '{"maxRewardsPerIp":2}',
    createdAt: '2026-08-01T00:00:00Z',
    updatedAt: '2026-08-02T00:00:00Z'
  }
  const form = referralProgramToForm(program)

  assert.equal(isReferralProgramFormChanged(form, program), false)

  form.name = 'Welcome Plus'
  assert.equal(isReferralProgramFormChanged(form, program), true)
  const payload = buildReferralProgramPayload(form)
  const rules = JSON.parse(payload.ruleDefinition)
  const rewards = JSON.parse(payload.rewardDefinition)

  assert.equal(payload.antiFraudSettings, '{"maxRewardsPerIp":2}')
  assert.equal(rules.campaign, 'summer')
  assert.equal(rewards.referrer.tier, 'gold')
  assert.equal(rewards.partner.type, 'cashback')
})

test('referral program form validates backend field boundaries', () => {
  const form = referralProgramToForm({
    id: 'program-1',
    revision: 3,
    name: 'A'.repeat(161),
    status: 'draft',
    startAt: null,
    endAt: null,
    ruleDefinition: '{}',
    rewardDefinition: '{"referrer":{"type":"bonus-days","value":1000001,"unit":"days"}}',
    antiFraudSettings: '{}',
    createdAt: '2026-08-01T00:00:00Z',
    updatedAt: '2026-08-02T00:00:00Z'
  })

  const errors = validateReferralProgramForm(form)

  assert.ok(errors.some((error) => error.includes('160')))
  assert.ok(errors.some((error) => error.includes('1 000 000')))
})
