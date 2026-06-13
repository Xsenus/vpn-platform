import { expect, test, type Page, type Route } from '@playwright/test'

const corsHeaders = {
  'access-control-allow-origin': '*',
  'access-control-allow-headers': 'content-type, authorization',
  'access-control-allow-methods': 'GET,POST,OPTIONS',
  'content-type': 'application/json; charset=utf-8'
}

const homeContent = [
  { key: 'home.hero.title', value: 'VPN без ручной выдачи' },
  { key: 'home.hero.subtitle', value: 'Публичный E2E проверяет главную, тарифы, FAQ и старт покупки.' },
  { key: 'home.hero.primaryCta', value: 'Выбрать тариф' },
  { key: 'home.pricing.title', value: 'Тарифы для проверки покупки' },
  { key: 'home.finalCta.title', value: 'Готовы купить VPN?' }
]

const faqItems = [
  {
    id: 'faq-payment',
    question: 'Как оплатить VPN?',
    answer: 'Выберите тариф и способ оплаты, затем создайте заказ.',
    category: 'Оплата',
    sortOrder: 1
  },
  {
    id: 'faq-connect',
    question: 'Когда появится подключение?',
    answer: 'После подтверждения платежа доступ появится в личном кабинете.',
    category: 'Подключение',
    sortOrder: 2
  }
]

const tariffs = [
  {
    id: 'tariff-start',
    name: 'Start 30 дней',
    slug: 'start-30',
    description: 'Базовый доступ для одного пользователя.',
    fullDescription: 'Публичный тариф для E2E проверки checkout.',
    features: ['2 устройства', 'Автовыдача после оплаты'],
    featuresJson: '[]',
    badge: 'Популярный',
    durationDays: 30,
    price: 299,
    currency: 'RUB',
    maxDevices: 2,
    trafficLimit: null,
    isTrial: false,
    isActive: true,
    sortOrder: 1,
    visibleFrom: null,
    visibleTo: null,
    tariffType: 'Personal',
    category: 'Личный',
    allowedRegionsCsv: 'EU',
    allowedNodeGroupsCsv: 'default',
    isReferralEligible: true,
    provisioningScenario: 'standard',
    afterPaymentText: 'После оплаты доступ появится автоматически.',
    createdAt: '2026-06-13T00:00:00Z',
    updatedAt: '2026-06-13T00:00:00Z'
  }
]

const paymentProviders = [
  {
    provider: 'YooKassa',
    publicName: 'YooKassa sandbox',
    mode: 'Sandbox',
    healthStatus: 'Healthy'
  }
]

function jsonResponse(body: unknown, status = 200) {
  return {
    status,
    headers: corsHeaders,
    body: JSON.stringify(body)
  }
}

async function fulfillJson(route: Route, body: unknown, status = 200) {
  await route.fulfill(jsonResponse(body, status))
}

async function mockPublicApi(page: Page) {
  let checkoutPayload: unknown = null

  await page.route('**/api/public/content/home', async (route) => {
    await fulfillJson(route, homeContent)
  })

  await page.route('**/api/public/content/faq**', async (route) => {
    await fulfillJson(route, faqItems)
  })

  await page.route('**/api/public/tariffs', async (route) => {
    await fulfillJson(route, tariffs)
  })

  await page.route('**/api/public/payments/providers', async (route) => {
    await fulfillJson(route, paymentProviders)
  })

  await page.route('**/api/public/checkout-sessions', async (route) => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: corsHeaders })
      return
    }

    checkoutPayload = route.request().postDataJSON()
    await fulfillJson(route, {
      id: 'checkout-session-1',
      token: 'public-checkout-token',
      tariffId: 'tariff-start',
      userId: null,
      orderId: null,
      status: 'PendingAuth',
      expiresAt: '2026-06-14T00:00:00Z',
      emailHint: null
    })
  })

  return {
    getCheckoutPayload: () => checkoutPayload
  }
}

test('public website covers landing, tariffs, FAQ and checkout start', async ({ page }) => {
  const consoleErrors: string[] = []
  page.on('console', (message) => {
    if (message.type() === 'error') consoleErrors.push(message.text())
  })
  page.on('pageerror', (error) => consoleErrors.push(error.message))

  const api = await mockPublicApi(page)

  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'VPN Platform' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'VPN без ручной выдачи' })).toBeVisible()
  await expect(page.getByText('Публичный E2E проверяет главную, тарифы, FAQ и старт покупки.')).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Как оплатить VPN?' })).toBeVisible()

  await page.getByRole('link', { name: 'Тарифы', exact: true }).click()
  await expect(page).toHaveURL(/\/tariffs$/)
  await expect(page.getByRole('heading', { name: 'Тарифы' })).toBeVisible()
  await expect(page.getByText('Start 30 дней')).toBeVisible()
  const providerSelect = page.getByLabel('Способ оплаты')
  await expect(providerSelect).toHaveValue('YooKassa')
  await expect(providerSelect.locator('option')).toContainText(['YooKassa sandbox · проверка'])

  await page.getByRole('button', { name: 'Купить' }).first().click()
  await expect(page).toHaveURL(/\/account$/)
  await expect(page.getByRole('heading', { name: 'Аккаунт' })).toBeVisible()
  await expect(page.getByText('Покупка сохранена')).toBeVisible()
  await expect(page.getByText('Start 30 дней')).toBeVisible()

  expect(api.getCheckoutPayload()).toMatchObject({
    tariffId: 'tariff-start',
    paymentProvider: 'YooKassa'
  })

  await page.getByRole('link', { name: 'FAQ' }).click()
  await expect(page).toHaveURL(/\/faq$/)
  await expect(page.getByRole('heading', { name: 'Вопросы и ответы' })).toBeVisible()
  await page.getByLabel('Поиск по FAQ').fill('подключение')
  await expect(page.getByText('Когда появится подключение?')).toBeVisible()
  await expect(page.getByText('Как оплатить VPN?')).not.toBeVisible()

  expect(consoleErrors).toEqual([])
})
