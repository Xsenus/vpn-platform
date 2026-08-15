import assert from 'node:assert/strict'
import test from 'node:test'
import type { AdminFaqItem, SiteContentBlockDto, WorkScenarioDto } from '../packages/api-client/src/index.ts'
import {
  isFaqFormChanged,
  isSiteContentFormChanged,
  isWorkScenarioFormChanged,
  normalizeFaqPayload,
  normalizeSiteContentPayload,
  normalizeWorkScenarioPayload
} from '../apps/admin-panel/src/admin-managed-editors.ts'

const now = '2026-08-15T00:00:00Z'

test('managed editor helpers detect only normalized FAQ, content and scenario changes', () => {
  const faq: AdminFaqItem = { id: 'faq-1', revision: 2, question: 'Как оплатить?', answer: 'Картой.', category: 'Оплата', isActive: true, showOnHome: true, showOnFaqPage: true, sortOrder: 10, createdAt: now, updatedAt: now }
  const faqForm = { question: ' Как оплатить? ', answer: ' Картой. ', category: ' Оплата ', isActive: true, showOnHome: true, showOnFaqPage: true, sortOrder: 10 }
  assert.deepEqual(normalizeFaqPayload(faqForm), { ...faqForm, question: faq.question, answer: faq.answer, category: faq.category })
  assert.equal(isFaqFormChanged(faqForm, faq), false)
  assert.equal(isFaqFormChanged({ ...faqForm, answer: 'Через СБП.' }, faq), true)

  const content: SiteContentBlockDto = { id: 'content-1', revision: 3, key: 'home.hero.title', value: 'Заголовок', group: 'home', label: 'Hero title', description: 'Первый экран', inputType: 'text', isActive: true, sortOrder: 20, createdAt: now, updatedAt: now }
  const contentForm = { key: ' home.hero.title ', value: 'Заголовок', group: ' home ', label: ' Hero title ', description: ' Первый экран ', inputType: ' text ', isActive: true, sortOrder: 20 }
  assert.deepEqual(normalizeSiteContentPayload(contentForm), { key: content.key, value: content.value, group: content.group, label: content.label, description: content.description, inputType: content.inputType, isActive: true, sortOrder: 20 })
  assert.equal(isSiteContentFormChanged(contentForm, content), false)
  assert.equal(isSiteContentFormChanged({ ...contentForm, value: 'Новый заголовок' }, content), true)

  const scenario: WorkScenarioDto = { id: 'scenario-1', revision: 4, name: 'Автовыдача', key: 'auto', isActive: true, allowedTariffIdsJson: '["tariff-1"]', vpnProtocol: 'vless', serverSelectionRule: 'least-loaded', inboundSelectionRule: 'default', provisioningMode: 'auto', onPaymentSucceeded: 'create_subscription_and_access', onPaymentFailed: 'keep_order_pending', onRefund: 'disable_access', onSubscriptionExpired: 'disable_access_after_grace', onRenewal: 'extend_subscription', cabinetText: 'Готово', telegramText: 'Готово', generateQrCode: true, maxDevices: 3, trafficLimit: null, sortOrder: 30, createdAt: now, updatedAt: now }
  const scenarioForm = { ...scenario, name: ' Автовыдача ', key: ' AUTO ', allowedTariffIdsJson: '["tariff-1","tariff-1"]', serverSelectionRule: ' least-loaded ', cabinetText: ' Готово ', telegramText: ' Готово ' }
  assert.deepEqual(normalizeWorkScenarioPayload(scenarioForm), normalizeWorkScenarioPayload(scenario))
  assert.equal(isWorkScenarioFormChanged(scenarioForm, scenario), false)
  assert.equal(normalizeWorkScenarioPayload({ ...scenarioForm, key: ' auto key! ' }).key, 'auto-key')
  assert.equal(isWorkScenarioFormChanged({ ...scenarioForm, maxDevices: 4 }, scenario), true)
})
