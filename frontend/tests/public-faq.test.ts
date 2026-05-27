import test from 'node:test'
import assert from 'node:assert/strict'
import { filterFaqItems, getFaqCategories, normalizeFaqCategory, FAQ_ALL_CATEGORY } from '../apps/public-web/src/faq-utils.ts'

const faqItems = [
  {
    id: 'faq-1',
    question: 'Как оплатить тариф?',
    answer: 'Выберите тариф и доступный способ оплаты.',
    category: 'Оплата',
    isActive: true,
    showOnHome: true,
    showOnFaqPage: true,
    sortOrder: 10
  },
  {
    id: 'faq-2',
    question: 'Как подключить VPN?',
    answer: 'Скопируйте ссылку из кабинета или отсканируйте QR-код.',
    category: 'Подключение',
    isActive: true,
    showOnHome: true,
    showOnFaqPage: true,
    sortOrder: 20
  },
  {
    id: 'faq-3',
    question: 'Что делать при продлении?',
    answer: 'Откройте кабинет и повторите оплату активного тарифа.',
    category: '',
    isActive: true,
    showOnHome: false,
    showOnFaqPage: true,
    sortOrder: 30
  }
]

test('public FAQ helpers normalize and sort categories for Russian UI', () => {
  assert.equal(normalizeFaqCategory(''), 'Общее')
  assert.equal(normalizeFaqCategory('  Оплата  '), 'Оплата')
  assert.deepEqual(getFaqCategories(faqItems), [FAQ_ALL_CATEGORY, 'Общее', 'Оплата', 'Подключение'])
})

test('public FAQ helpers filter by category and full text search', () => {
  assert.equal(filterFaqItems(faqItems, FAQ_ALL_CATEGORY, 'qr').map((item) => item.id).join(','), 'faq-2')
  assert.equal(filterFaqItems(faqItems, 'Оплата', 'тариф').map((item) => item.id).join(','), 'faq-1')
  assert.equal(filterFaqItems(faqItems, 'Общее', 'продлении').map((item) => item.id).join(','), 'faq-3')
  assert.deepEqual(filterFaqItems(faqItems, 'Подключение', 'оплата'), [])
})
