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
].map((item, index) => ({
  id: `content-${index + 1}`,
  ...item,
  group: 'home',
  label: item.key,
  description: '',
  inputType: 'text',
  isActive: true,
  sortOrder: index + 1,
  createdAt: '2026-06-13T00:00:00Z',
  updatedAt: '2026-06-13T00:00:00Z'
}))

const faqItems = [
  {
    id: 'faq-payment',
    question: 'Как оплатить VPN?',
    answer: 'Выберите тариф и способ оплаты, затем создайте заказ.',
    category: 'Оплата',
    isActive: true,
    showOnHome: true,
    showOnFaqPage: true,
    sortOrder: 1,
    createdAt: '2026-06-13T00:00:00Z',
    updatedAt: '2026-06-13T00:00:00Z'
  },
  {
    id: 'faq-connect',
    question: 'Когда появится подключение?',
    answer: 'После подтверждения платежа доступ появится в личном кабинете.',
    category: 'Подключение',
    isActive: true,
    showOnHome: true,
    showOnFaqPage: true,
    sortOrder: 2,
    createdAt: '2026-06-13T00:00:00Z',
    updatedAt: '2026-06-13T00:00:00Z'
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
  let logoutPayload: unknown = null
  let logoutAuthorization = ''
  let logoutShouldFail = false
  let refreshPayload: unknown = null
  let rejectNextProfileRequest = true
  let rejectNextCheckoutPromo = false
  let checkoutDelayMs = 0
  let checkoutClaimDelayMs = 0
  let checkoutRequestCount = 0
  let checkoutClaimRequestCount = 0
  let paymentInitRequestCount = 0
  let unsafePaymentLink = false
  let invalidCheckoutResponse = false
  let invalidAuthResponse = false
  let invalidTariffsResponse = false
  let wrongShapeTariffsResponse = false
  let invalidItemTariffsResponse = false
  let oversizedTariffsResponse = false
  let failNextPaymentInit = false
  let paymentInitDelayMs = 0

  await page.route('**/api/public/content/home', async (route) => {
    await fulfillJson(route, homeContent)
  })

  await page.route('**/api/public/content/faq**', async (route) => {
    await fulfillJson(route, faqItems)
  })

  await page.route('**/api/public/tariffs', async (route) => {
    if (oversizedTariffsResponse) {
      await route.fulfill({
        status: 200,
        headers: corsHeaders,
        body: JSON.stringify({ padding: 'x'.repeat(10_000_000) })
      })
      return
    }
    if (wrongShapeTariffsResponse) {
      await fulfillJson(route, {})
      return
    }
    if (invalidItemTariffsResponse) {
      await fulfillJson(route, [{}])
      return
    }
    if (invalidTariffsResponse) {
      await route.fulfill({
        status: 200,
        headers: { ...corsHeaders, 'content-type': 'text/html; charset=utf-8' },
        body: '<html><body>Proxy login</body></html>'
      })
      return
    }
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
    checkoutRequestCount += 1
    if (checkoutDelayMs > 0) {
      const delay = checkoutDelayMs
      checkoutDelayMs = 0
      await new Promise((resolve) => setTimeout(resolve, delay))
    }
    if (rejectNextCheckoutPromo) {
      rejectNextCheckoutPromo = false
      await fulfillJson(route, { error: 'Promo code not found.' }, 400)
      return
    }

    if (invalidCheckoutResponse) {
      invalidCheckoutResponse = false
      await fulfillJson(route, {})
      return
    }

    await fulfillJson(route, {
      id: 'checkout-session-1',
      token: 'public-checkout-token',
      tariffId: 'tariff-start',
      userId: null,
      orderId: null,
      status: 'open',
      expiresAt: '2026-06-14T00:00:00Z',
      emailHint: null
    })
  })

  await page.route('**/api/auth/register', async (route) => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: corsHeaders })
      return
    }
    if (invalidAuthResponse) {
      invalidAuthResponse = false
      await fulfillJson(route, {})
      return
    }
    await fulfillJson(route, {
      accessToken: 'public-access-token',
      refreshToken: 'public-refresh-token',
      expiresAt: '2026-06-13T08:00:00Z',
      userId: 'public-user',
      email: 'public@example.test',
      displayName: 'Public E2E'
    })
  })

  await page.route('**/api/auth/login', async (route) => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: corsHeaders })
      return
    }
    await fulfillJson(route, {
      accessToken: 'public-access-token-2',
      refreshToken: 'public-refresh-token-2',
      expiresAt: '2026-06-13T08:00:00Z',
      userId: 'public-user',
      email: 'public@example.test',
      displayName: 'Public E2E'
    })
  })

  await page.route('**/api/auth/forgot-password', async (route) => {
    await fulfillJson(route, {
      accepted: true,
      message: 'If the account exists, a password reset instruction has been queued for the configured delivery channel.',
      validationResetToken: 'public-reset-token'
    })
  })

  await page.route('**/api/auth/reset-password', async (route) => {
    await fulfillJson(route, { status: 'password_changed' })
  })

  await page.route('**/api/auth/refresh', async (route) => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: corsHeaders })
      return
    }
    refreshPayload = route.request().postDataJSON()
    await fulfillJson(route, {
      accessToken: 'public-refreshed-access-token',
      refreshToken: 'public-refreshed-refresh-token',
      expiresAt: '2026-06-13T09:00:00Z',
      userId: 'public-user',
      email: 'public@example.test',
      displayName: 'Public E2E'
    })
  })

  await page.route('**/api/me', async (route) => {
    if (rejectNextProfileRequest) {
      rejectNextProfileRequest = false
      await fulfillJson(route, { error: 'expired access token' }, 401)
      return
    }
    await fulfillJson(route, {
      id: 'public-user',
      email: 'public@example.test',
      displayName: 'Public E2E',
      preferredLanguage: 'ru',
      referralCode: 'PUBLIC-E2E',
      status: 'Active'
    })
  })

  await page.route('**/api/me/checkout-sessions/public-checkout-token/claim', async (route) => {
    checkoutClaimRequestCount += 1
    if (checkoutClaimDelayMs > 0) {
      await new Promise((resolve) => setTimeout(resolve, checkoutClaimDelayMs))
    }
    await fulfillJson(route, {
      id: 'public-order',
      userId: 'public-user',
      tariffId: 'tariff-start',
      tariffName: 'Start 30 дней',
      amount: 299,
      currency: 'RUB',
      status: 'PendingPayment',
      expiresAt: '2026-06-14T00:00:00Z',
      linkedSubscriptionId: null
    })
  })

  await page.route('**/api/me/orders/public-order/payments/YooKassa/init', async (route) => {
    paymentInitRequestCount += 1
    if (paymentInitDelayMs > 0) {
      const delay = paymentInitDelayMs
      paymentInitDelayMs = 0
      await new Promise((resolve) => setTimeout(resolve, delay))
    }
    if (failNextPaymentInit) {
      failNextPaymentInit = false
      await fulfillJson(route, { error: 'payment provider unavailable' }, 503)
      return
    }
    await fulfillJson(route, {
      paymentId: 'public-payment',
      redirectUrl: unsafePaymentLink ? 'javascript:alert(1)' : 'https://pay.example.test/public',
      rawResponse: '{"sandbox":true}'
    })
  })

  await page.route('**/api/auth/logout', async (route) => {
    if (route.request().method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: corsHeaders })
      return
    }
    logoutPayload = route.request().postDataJSON()
    logoutAuthorization = route.request().headers().authorization ?? ''
    await fulfillJson(route, logoutShouldFail ? { error: 'logout unavailable' } : { status: 'ok' }, logoutShouldFail ? 503 : 200)
  })

  return {
    getCheckoutPayload: () => checkoutPayload,
    getLogoutPayload: () => logoutPayload,
    getLogoutAuthorization: () => logoutAuthorization,
    getRefreshPayload: () => refreshPayload,
    failLogout: () => { logoutShouldFail = true },
    rejectCheckoutPromo: () => { rejectNextCheckoutPromo = true },
    delayNextCheckout: (delayMs: number) => { checkoutDelayMs = delayMs },
    delayCheckoutClaim: (delayMs: number) => { checkoutClaimDelayMs = delayMs },
    failNextPaymentInit: () => { failNextPaymentInit = true },
    delayNextPaymentInit: (delayMs: number) => { paymentInitDelayMs = delayMs },
    getCheckoutRequestCounts: () => ({
      checkout: checkoutRequestCount,
      claim: checkoutClaimRequestCount,
      paymentInit: paymentInitRequestCount
    }),
    returnUnsafePaymentLink: () => { unsafePaymentLink = true },
    returnInvalidCheckoutResponse: () => { invalidCheckoutResponse = true },
    returnInvalidAuthResponse: () => { invalidAuthResponse = true },
    returnInvalidTariffsResponse: () => { invalidTariffsResponse = true },
    returnWrongShapeTariffsResponse: () => {
      invalidTariffsResponse = false
      wrongShapeTariffsResponse = true
    },
    returnInvalidItemTariffsResponse: () => {
      invalidTariffsResponse = false
      wrongShapeTariffsResponse = false
      invalidItemTariffsResponse = true
    },
    returnOversizedTariffsResponse: () => {
      invalidTariffsResponse = false
      wrongShapeTariffsResponse = false
      invalidItemTariffsResponse = false
      oversizedTariffsResponse = true
    }
  }
}

test('public tariffs handles invalid MIME, shape, item and size without a render crash', async ({ page }) => {
  const pageErrors: string[] = []
  page.on('pageerror', (error) => pageErrors.push(error.message))
  const api = await mockPublicApi(page)
  api.returnInvalidTariffsResponse()

  await page.goto('/tariffs')

  await expect(page.getByRole('heading', { name: 'Тарифы' })).toBeVisible()
  await expect(page.getByRole('alert')).toContainText('Не удалось загрузить тарифы')
  await expect(page.getByText('Start 30 дней')).toHaveCount(0)

  api.returnWrongShapeTariffsResponse()
  await page.reload()
  await expect(page.getByRole('heading', { name: 'Тарифы' })).toBeVisible()
  await expect(page.getByRole('alert')).toContainText('Не удалось загрузить тарифы')
  await expect(page.getByText('Start 30 дней')).toHaveCount(0)

  api.returnInvalidItemTariffsResponse()
  await page.reload()
  await expect(page.getByRole('heading', { name: 'Тарифы' })).toBeVisible()
  await expect(page.getByRole('alert')).toContainText('Не удалось загрузить тарифы')
  await expect(page.getByText('Start 30 дней')).toHaveCount(0)

  api.returnOversizedTariffsResponse()
  await page.reload()
  await expect(page.getByRole('heading', { name: 'Тарифы' })).toBeVisible()
  await expect(page.getByRole('alert')).toContainText('Не удалось загрузить тарифы')
  await expect(page.getByText('Start 30 дней')).toHaveCount(0)
  expect(pageErrors).toEqual([])
})

test('public checkout and auth reject malformed success DTOs without persisting stale state', async ({ page }) => {
  const pageErrors: string[] = []
  page.on('pageerror', (error) => pageErrors.push(error.message))
  const api = await mockPublicApi(page)

  await page.goto('/tariffs')
  api.returnInvalidCheckoutResponse()
  await page.getByRole('button', { name: 'Купить' }).first().click()
  await expect(page).toHaveURL(/\/tariffs$/)
  await expect(page.getByRole('alert')).toContainText('Сервер вернул JSON-ответ с некорректными данными')
  await expect.poll(() => page.evaluate(() => sessionStorage.getItem('vpn-platform-pending-checkout'))).toBeNull()

  await page.getByRole('button', { name: 'Купить' }).first().click()
  await expect(page).toHaveURL(/\/account$/)
  await page.getByRole('tab', { name: 'Регистрация' }).click()
  const authPanel = page.locator('#public-auth-panel')
  await authPanel.getByLabel('Имя').fill('Malformed DTO')
  await authPanel.getByLabel('Email').fill('malformed@example.test')
  await authPanel.getByRole('textbox', { name: 'Пароль', exact: true }).fill('Password123!')
  api.returnInvalidAuthResponse()
  await authPanel.getByRole('button', { name: 'Создать аккаунт' }).click()
  await expect(page.getByRole('alert')).toContainText('Сервер вернул JSON-ответ с некорректными данными')
  await expect.poll(() => page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-public-token'),
    refresh: sessionStorage.getItem('vpn-platform-public-refresh-token')
  }))).toEqual({ access: null, refresh: null })
  await expect(page.getByText('Malformed DTO').first()).toHaveCount(0)
  expect(pageErrors).toEqual([])
})

test('public account discards an unsafe persisted checkout before authorized requests', async ({ page }) => {
  const pageErrors: string[] = []
  page.on('pageerror', (error) => pageErrors.push(error.message))
  await page.addInitScript(() => {
    sessionStorage.setItem('vpn-platform-public-token', 'public-access-token')
    sessionStorage.setItem('vpn-platform-public-refresh-token', 'public-refresh-token')
    sessionStorage.setItem('vpn-platform-pending-checkout', JSON.stringify({
      token: 'checkout_token_1234567890123456789012345678',
      tariffName: 'Injected tariff',
      provider: '../../auth/logout'
    }))
  })
  const api = await mockPublicApi(page)

  await page.goto('/account')
  await expect(page.getByText('Public E2E').first()).toBeVisible()
  await expect(page.getByText('Injected tariff')).toHaveCount(0)
  await expect.poll(api.getCheckoutRequestCounts).toEqual({ checkout: 0, claim: 0, paymentInit: 0 })
  await expect.poll(() => page.evaluate(() => sessionStorage.getItem('vpn-platform-pending-checkout'))).toBeNull()
  expect(pageErrors).toEqual([])
})

test('authenticated public checkout owns one claim and payment initialization', async ({ page }) => {
  await page.addInitScript(() => {
    sessionStorage.setItem('vpn-platform-public-token', 'public-access-token')
    sessionStorage.setItem('vpn-platform-public-refresh-token', 'public-refresh-token')
  })
  const api = await mockPublicApi(page)
  api.delayCheckoutClaim(150)
  api.failNextPaymentInit()

  await page.goto('/tariffs')
  await expect(page.getByRole('link', { name: /Привет, Public E2E/ })).toBeVisible()
  await page.getByRole('button', { name: 'Купить' }).first().click()

  await expect(page).toHaveURL(/\/account$/)
  await expect(page.getByRole('alert').filter({ hasText: 'payment provider unavailable' })).toBeVisible()
  await expect(page.getByText('ID заказа: public-order')).toBeVisible()
  await expect.poll(api.getCheckoutRequestCounts).toEqual({ checkout: 1, claim: 1, paymentInit: 1 })
  await page.waitForTimeout(250)
  expect(api.getCheckoutRequestCounts()).toEqual({ checkout: 1, claim: 1, paymentInit: 1 })
  await page.getByRole('button', { name: 'Повторить оплату' }).first().click()
  await expect(page.getByRole('heading', { name: 'Последняя покупка' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Открыть оплату в новой вкладке' })).toBeVisible()
  await expect.poll(api.getCheckoutRequestCounts).toEqual({ checkout: 1, claim: 1, paymentInit: 2 })
  await expect.poll(() => page.evaluate(() => sessionStorage.getItem('vpn-platform-pending-checkout'))).toBeNull()
})

test('public checkout ignores a late payment response after logout', async ({ page }) => {
  await page.addInitScript(() => {
    sessionStorage.setItem('vpn-platform-public-token', 'public-access-token')
    sessionStorage.setItem('vpn-platform-public-refresh-token', 'public-refresh-token')
  })
  const api = await mockPublicApi(page)
  api.delayNextPaymentInit(500)

  await page.goto('/tariffs')
  await expect(page.getByRole('link', { name: /Привет, Public E2E/ })).toBeVisible()
  await page.getByRole('button', { name: 'Купить' }).first().click()
  await expect(page).toHaveURL(/\/account$/)
  await expect.poll(api.getCheckoutRequestCounts).toEqual({ checkout: 1, claim: 1, paymentInit: 1 })
  await page.getByRole('button', { name: 'Выйти' }).click()

  await expect(page.getByRole('tab', { name: 'Вход' })).toBeVisible()
  await page.waitForTimeout(600)
  await expect(page.getByRole('heading', { name: 'Последняя покупка' })).toHaveCount(0)
  await expect(page.getByRole('link', { name: 'Открыть оплату в новой вкладке' })).toHaveCount(0)
  await expect.poll(() => page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-public-token'),
    refresh: sessionStorage.getItem('vpn-platform-public-refresh-token')
  }))).toEqual({ access: null, refresh: null })
})

test('public website covers landing, tariffs, FAQ and checkout start', async ({ page }, testInfo) => {
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

  api.rejectCheckoutPromo()
  api.delayNextCheckout(300)
  await page.getByLabel('Промокод').fill('UNKNOWN')
  await page.getByRole('button', { name: 'Купить' }).first().click()
  await expect(providerSelect).toBeDisabled()
  await expect(page.getByLabel('Промокод')).toBeDisabled()
  await expect(page.getByRole('button', { name: 'Создаем заказ...' })).toBeDisabled()
  await expect(page).toHaveURL(/\/tariffs$/)
  await expect(page.getByText('Промокод не найден. Проверьте написание.')).toBeVisible()
  await expect(providerSelect).toBeEnabled()
  await expect(page.getByLabel('Промокод')).toBeEnabled()
  const expectedPromoFailureLogs = consoleErrors.filter((message) => message.includes('400 (Bad Request)'))
  expect(expectedPromoFailureLogs.length).toBeGreaterThan(0)
  for (const message of expectedPromoFailureLogs) consoleErrors.splice(consoleErrors.indexOf(message), 1)
  await page.getByLabel('Промокод').fill('')

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

  await page.getByRole('link', { name: 'Аккаунт' }).click()
  await page.getByRole('tab', { name: 'Регистрация' }).click()
  const authPanel = page.locator('#public-auth-panel')
  await authPanel.getByLabel('Имя').fill('Public E2E')
  await authPanel.getByLabel('Email').fill('public@example.test')
  await authPanel.getByRole('textbox', { name: 'Пароль', exact: true }).fill('Password123!')
  await authPanel.getByLabel('Реферальный код').fill('PUBLIC-REF')
  api.returnUnsafePaymentLink()
  const registrationRequestPromise = page.waitForRequest((request) => request.method() === 'POST' && new URL(request.url()).pathname === '/api/auth/register')
  await authPanel.getByRole('button', { name: 'Создать аккаунт' }).click()
  const registrationRequest = await registrationRequestPromise
  expect(registrationRequest.postDataJSON()).toMatchObject({ referralCode: 'PUBLIC-REF' })
  await expect(page.getByText('Public E2E').first()).toBeVisible()
  const rejectedPaymentLinkAlert = page.getByRole('alert').filter({ hasText: 'Сервер вернул JSON-ответ с некорректными данными' })
  await expect(rejectedPaymentLinkAlert).toBeVisible()
  await expect(page.getByRole('link', { name: 'Открыть оплату в новой вкладке' })).toHaveCount(0)
  await expect(page.getByRole('button', { name: /Скопировать ссылку: скопировать значение/ })).toHaveCount(0)
  await expect.poll(api.getCheckoutRequestCounts).toEqual({ checkout: 2, claim: 1, paymentInit: 1 })
  await page.waitForTimeout(250)
  expect(api.getCheckoutRequestCounts()).toEqual({ checkout: 2, claim: 1, paymentInit: 1 })
  expect(api.getRefreshPayload()).toEqual({ refreshToken: 'public-refresh-token' })
  await expect.poll(() => page.evaluate(() => sessionStorage.getItem('vpn-platform-public-refresh-token'))).toBe('public-refreshed-refresh-token')
  const expectedRefreshLogs = consoleErrors.filter((message) => message.includes('401 (Unauthorized)'))
  expect(expectedRefreshLogs.length).toBeGreaterThan(0)
  for (const message of expectedRefreshLogs) consoleErrors.splice(consoleErrors.indexOf(message), 1)

  await page.getByRole('button', { name: 'Выйти' }).click()
  await expect(page.getByRole('tab', { name: 'Вход' })).toBeVisible()
  expect(api.getLogoutPayload()).toEqual({ refreshToken: 'public-refreshed-refresh-token' })
  expect(api.getLogoutAuthorization()).toBe('Bearer public-refreshed-access-token')
  await expect.poll(() => page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-public-token'),
    refresh: sessionStorage.getItem('vpn-platform-public-refresh-token')
  }))).toEqual({ access: null, refresh: null })

  const resetCard = page.getByRole('heading', { name: 'Сброс пароля' }).locator('..')
  await page.evaluate(() => {
    sessionStorage.setItem('vpn-platform-public-token', 'stale-public-access-token')
    sessionStorage.setItem('vpn-platform-public-refresh-token', 'stale-public-refresh-token')
  })
  await resetCard.getByLabel('Email').fill('public@example.test')
  await resetCard.getByRole('button', { name: 'Запросить код' }).click()
  await resetCard.getByRole('textbox', { name: 'Новый пароль', exact: true }).fill('ChangedPassword123!')
  await resetCard.getByRole('button', { name: 'Изменить пароль' }).click()
  await expect(page.getByText('Пароль изменён. Войдите с новым паролем.')).toBeVisible()
  await expect(page.getByRole('tab', { name: 'Вход' })).toBeVisible()
  await expect.poll(() => page.evaluate(() => ({
    access: sessionStorage.getItem('vpn-platform-public-token'),
    refresh: sessionStorage.getItem('vpn-platform-public-refresh-token')
  }))).toEqual({ access: null, refresh: null })

  await page.getByRole('tab', { name: 'Вход' }).click()
  await authPanel.getByLabel('Email').fill('public@example.test')
  await authPanel.getByRole('textbox', { name: 'Пароль', exact: true }).fill('ChangedPassword123!')
  await authPanel.getByRole('button', { name: 'Войти' }).click()
  await expect(page.getByText('Public E2E').first()).toBeVisible()
  api.failLogout()
  await page.getByRole('button', { name: 'Выйти' }).click()
  await expect(page.getByText('Локальная сессия завершена, но отзыв серверной сессии не подтверждён. На чужом устройстве измените пароль из доверенного браузера.')).toBeVisible()
  await expect.poll(() => page.evaluate(() => sessionStorage.getItem('vpn-platform-public-refresh-token'))).toBeNull()
  const expectedLogoutFailureLogs = consoleErrors.filter((message) => message.includes('503 (Service Unavailable)'))
  expect(expectedLogoutFailureLogs.length).toBeGreaterThan(0)
  for (const message of expectedLogoutFailureLogs) consoleErrors.splice(consoleErrors.indexOf(message), 1)

  if (testInfo.project.name.startsWith('mobile-')) {
    await page.screenshot({ path: testInfo.outputPath('public-mobile.png'), fullPage: true })
  }

  expect(consoleErrors).toEqual([])
})
