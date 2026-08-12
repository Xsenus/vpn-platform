import test from 'node:test'
import assert from 'node:assert/strict'
import { filterFaqItems, getFaqCategories, normalizeFaqCategory, FAQ_ALL_CATEGORY } from '../apps/public-web/src/faq-utils.ts'

const faqItems = [
  {
    question: 'Как оплатить тариф?',
    answer: 'Выберите тариф и доступный способ оплаты.',
    category: 'Оплата'
  },
  {
    question: 'Как подключить VPN?',
    answer: 'Скопируйте ссылку из кабинета или отсканируйте QR-код.',
    category: 'Подключение'
  },
  {
    question: 'Что делать при продлении?',
    answer: 'Откройте кабинет и повторите оплату активного тарифа.',
    category: ''
  }
]

test('public FAQ helpers normalize and sort categories for Russian UI', () => {
  assert.equal(normalizeFaqCategory(''), 'Общее')
  assert.equal(normalizeFaqCategory('  Оплата  '), 'Оплата')
  assert.deepEqual(getFaqCategories(faqItems), [FAQ_ALL_CATEGORY, 'Общее', 'Оплата', 'Подключение'])
})

test('public FAQ helpers filter by category and full text search', () => {
  assert.equal(filterFaqItems(faqItems, FAQ_ALL_CATEGORY, 'qr').map((item) => item.question).join(','), 'Как подключить VPN?')
  assert.equal(filterFaqItems(faqItems, 'Оплата', 'тариф').map((item) => item.question).join(','), 'Как оплатить тариф?')
  assert.equal(filterFaqItems(faqItems, 'Общее', 'продлении').map((item) => item.question).join(','), 'Что делать при продлении?')
  assert.deepEqual(filterFaqItems(faqItems, 'Подключение', 'оплата'), [])
})
