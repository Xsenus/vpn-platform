import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Link, NavLink, Route, Routes, useLocation, useNavigate } from 'react-router'
import {
  ApiClient,
  AuthResponse,
  FaqItem,
  OrderDto,
  PaymentInitResult,
  PaymentProvider,
  PublicPaymentProviderDto,
  SiteContentBlockDto,
  TariffDto,
  UserProfileDto,
  translateAuthError,
  translateAuthMessage,
  validateAuthInput,
  validatePasswordResetConfirm,
  validatePasswordResetRequest
} from '@vpn-platform/api-client'
import { Card, EmptyState, ErrorBlock, ExternalLinkActions, LoadingBlock, PageShell, PasswordField, PrimaryButton, SegmentedTabs, SkipLink, StatTile, StatusBadge, ValidationModeBadge } from '@vpn-platform/ui'
import { FAQ_ALL_CATEGORY, filterFaqItems, getFaqCategories, normalizeFaqCategory } from './faq-utils'
import { parsePendingCheckout, type PendingCheckout } from './pending-checkout'
import { canStartCheckout, getCheckoutErrorMessage, getCheckoutUnavailableReason, getPublicListState, getTariffFeatures as tariffFeatures } from './public-page-state'
import { getPublicRouteMetadata } from './public-route'
import { getPublicSessionCheckError, isPublicAccessTokenExpired, isPublicSessionRejected, publicSessionEndedMessage } from './public-session'

const api = new ApiClient(import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080')
const TOKEN_STORAGE_KEY = 'vpn-platform-public-token'
const REFRESH_TOKEN_STORAGE_KEY = 'vpn-platform-public-refresh-token'
const PENDING_CHECKOUT_STORAGE_KEY = 'vpn-platform-pending-checkout'

type CheckoutState = {
  tariffName: string
  provider: PaymentProvider
  order: OrderDto
  payment: PaymentInitResult
} | null

function readSessionStorageItem(key: string) {
  try {
    return typeof sessionStorage === 'undefined' ? null : sessionStorage.getItem(key)
  } catch {
    return null
  }
}

function writeSessionStorageItem(key: string, value: string) {
  try {
    if (typeof sessionStorage !== 'undefined') sessionStorage.setItem(key, value)
  } catch {
    // Private browsing and embedded environments can block Web Storage.
  }
}

function removeSessionStorageItem(key: string) {
  try {
    if (typeof sessionStorage !== 'undefined') sessionStorage.removeItem(key)
  } catch {
    // Storage cleanup is best-effort when browser storage is unavailable.
  }
}

function applyPublicPageMetadata(title: string, description: string) {
  document.title = title
  const meta = document.querySelector('meta[name="description"]') ?? document.head.appendChild(document.createElement('meta'))
  meta.setAttribute('name', 'description')
  meta.setAttribute('content', description)
}

function PublicRouteEffects() {
  const { pathname } = useLocation()
  const previousPathname = useRef(pathname)

  useEffect(() => {
    const metadata = getPublicRouteMetadata(pathname)
    applyPublicPageMetadata(metadata.title, metadata.description)

    if (previousPathname.current === pathname) return
    previousPathname.current = pathname
    window.scrollTo({ top: 0, left: 0, behavior: 'auto' })
    const focusFrame = window.requestAnimationFrame(() => {
      document.getElementById('main-content')?.focus({ preventScroll: true })
    })

    return () => window.cancelAnimationFrame(focusFrame)
  }, [pathname])

  return null
}

function readPendingCheckout(): PendingCheckout | null {
  const raw = readSessionStorageItem(PENDING_CHECKOUT_STORAGE_KEY)
  if (!raw) return null

  const pendingCheckout = parsePendingCheckout(raw)
  if (!pendingCheckout) {
    removeSessionStorageItem(PENDING_CHECKOUT_STORAGE_KEY)
  }

  return pendingCheckout
}

const landingPlans = [
  {
    name: 'Start',
    price: 'от 299 ₽',
    description: 'Для одного пользователя и быстрой проверки сервиса.',
    features: ['1-2 устройства', 'Автовыдача доступа', 'Личный кабинет']
  },
  {
    name: 'Standard',
    price: 'от 490 ₽',
    description: 'Оптимальный тариф для регулярного использования.',
    features: ['До 3 устройств', 'Telegram-уведомления', 'Промокоды и продления']
  },
  {
    name: 'Premium',
    price: 'от 790 ₽',
    description: 'Для семьи, команды или нескольких устройств.',
    features: ['До 5 устройств', 'Приоритетные серверы', 'Быстрая поддержка']
  }
]

const defaultHomeContent: Record<string, string> = {
  'home.hero.eyebrow': 'VPN Platform',
  'home.hero.title': 'Быстрый VPN-доступ с оплатой и автоматической выдачей',
  'home.hero.subtitle': 'Выберите тариф, оплатите удобным способом и получите готовую ссылку подключения. Платформа объединяет витрину, личный кабинет, Telegram-бота, платежи, тарифы и управление серверами.',
  'home.hero.primaryCta': 'Выбрать тариф',
  'home.hero.secondaryCta': 'Войти или зарегистрироваться',
  'home.seo.title': 'VPN Platform — быстрый VPN-доступ с автоматической выдачей',
  'home.seo.description': 'Купите VPN-доступ онлайн: тарифы, оплата, личный кабинет, Telegram-бот и автоматическая выдача подключения.',
  'home.features.title': 'Все ключевые сценарии продажи VPN в одной системе',
  'home.features.subtitle': 'Лендинг ведет пользователя к тарифу, кабинет помогает завершить покупку, а админка дает контроль над тарифами, провайдерами, ботами, серверами и выдачей доступа.',
  'home.features.item1': 'Автоматическая выдача VPN-доступа после подтверждения оплаты.',
  'home.features.item2': 'Тарифы, платежи, Telegram-боты и серверы управляются из админки.',
  'home.features.item3': 'Поддержка нескольких платежных провайдеров и безопасного sandbox-режима.',
  'home.features.item4': 'Личный кабинет хранит заказы, ссылки подключения и статус подписки.',
  'home.pricing.title': 'Понятные планы для разных сценариев',
  'home.network.title': 'Глобальная логика VPN-сервиса без ручной рутины',
  'home.network.subtitle': 'Подключайте свои VPS, панели 3x-ui и правила выдачи доступов. Пользователь видит простой продукт, администратор управляет инфраструктурой.',
  'home.testimonials.title': 'Пользовательский путь остается простым',
  'home.testimonials.item1.name': 'Алексей',
  'home.testimonials.item1.role': 'предприниматель',
  'home.testimonials.item1.text': 'Оплатил тариф, получил ссылку подключения и сразу добавил ее на телефон и ноутбук.',
  'home.testimonials.item2.name': 'Марина',
  'home.testimonials.item2.role': 'удаленная работа',
  'home.testimonials.item2.text': 'Понравилось, что не нужно писать в поддержку после оплаты: доступ появляется автоматически.',
  'home.testimonials.item3.name': 'Игорь',
  'home.testimonials.item3.role': 'администратор сервиса',
  'home.testimonials.item3.text': 'В админке видно пользователей, платежи, тарифы и состояние VPN-серверов в одном месте.',
  'home.finalCta.title': 'Готовы проверить покупку VPN?',
  'home.finalCta.subtitle': 'Начните с тарифа или войдите в кабинет, чтобы привязать заказ и получить ссылку подключения.',
  'home.footer.text': 'VPN Platform объединяет продажи, оплату, выдачу и поддержку VPN-доступов в одном интерфейсе.',
  'home.footer.support': 'Поддержка доступна через личный кабинет и Telegram-бота.',
  'home.errors.tariffsLoad': 'Не удалось загрузить тарифы. Обновите страницу или попробуйте позже.',
  'home.errors.paymentProvidersLoad': 'Не удалось загрузить способы оплаты. Покупка временно недоступна.',
  'home.errors.noPaymentProviders': 'Нет доступных платежных провайдеров. Попробуйте позже или обратитесь в поддержку.',
  'home.errors.checkoutCreate': 'Не удалось создать покупку.',
  'home.checkout.unavailable.loading': 'Загружаем способы оплаты...',
  'home.checkout.unavailable.noProviders': 'Оплата временно недоступна: нет включенных способов оплаты.',
  'home.checkout.unavailable.chooseProvider': 'Выберите способ оплаты перед покупкой.',
  'home.checkout.providersEmptyTitle': 'Нет доступных способов оплаты',
  'home.checkout.providersEmptyDescription': 'Покупка временно недоступна: нет включенного и настроенного способа оплаты.',
  'home.checkout.settingsHint': 'Если вы еще не вошли, мы сохраним выбранный тариф и попросим авторизоваться перед оплатой.',
  'home.checkout.pendingAuthNotice': 'Покупка создана. Войдите или зарегистрируйтесь, чтобы привязать заказ и перейти к оплате.',
  'home.checkout.resultTitle': 'Последняя покупка',
  'home.checkout.afterPaymentText': 'После оплаты вернитесь в кабинет: статус заказа обновится автоматически, а VPN-доступ появится после подтверждения платежа.',
  'home.checkout.openPaymentCta': 'Открыть оплату',
  'home.checkout.copyPaymentLink': 'Скопировать ссылку'
}

function mapContent(blocks: SiteContentBlockDto[]) {
  return blocks.reduce<Record<string, string>>((acc, block) => {
    if (block.key && block.value) acc[block.key] = block.value
    return acc
  }, {})
}

function LandingHomePage({ profile }: { profile: UserProfileDto | null }) {
  const [homeFaq, setHomeFaq] = useState<FaqItem[]>([])
  const [homeContent, setHomeContent] = useState<Record<string, string>>(defaultHomeContent)

  useEffect(() => {
    api.getHomeFaq().then((items) => setHomeFaq(items.slice(0, 4))).catch(() => setHomeFaq([]))
    api.getHomeContent().then((items) => setHomeContent({ ...defaultHomeContent, ...mapContent(items) })).catch(() => setHomeContent(defaultHomeContent))
  }, [])

  const content = (key: string) => homeContent[key] ?? defaultHomeContent[key] ?? ''
  const featureItems = [1, 2, 3, 4].map((index) => content(`home.features.item${index}`)).filter(Boolean)
  const testimonialItems = [1, 2, 3].map((index) => ({
    name: content(`home.testimonials.item${index}.name`),
    role: content(`home.testimonials.item${index}.role`),
    text: content(`home.testimonials.item${index}.text`)
  })).filter((item) => item.name && item.text)

  useEffect(() => {
    applyPublicPageMetadata(content('home.seo.title') || 'VPN Platform', content('home.seo.description'))
  }, [homeContent])

  return (
    <PageShell title="VPN Platform">
      <section className="landing-hero" id="about">
        <div className="landing-hero-copy">
          <p className="eyebrow">{content('home.hero.eyebrow')}</p>
          <h2>{content('home.hero.title')}</h2>
          <p>{content('home.hero.subtitle')}</p>
          <div className="hero-actions">
            <Link to="/tariffs" className="button">{content('home.hero.primaryCta')}</Link>
            <Link to="/account" className="button button-ghost">{profile ? 'Открыть кабинет' : content('home.hero.secondaryCta')}</Link>
          </div>
        </div>
        <div className="landing-hero-visual" aria-label="Схема автоматической выдачи VPN">
          <div className="network-orbit">
            <span className="network-node node-main">VPN</span>
            <span className="network-node node-a">EU</span>
            <span className="network-node node-b">US</span>
            <span className="network-node node-c">TR</span>
            <span className="network-node node-d">SG</span>
          </div>
          <div className="connection-card">
            <span className="status-dot" />
            <div>
              <strong>Доступ готов</strong>
              <small>оплата {'>'} подписка {'>'} ссылка</small>
            </div>
          </div>
        </div>
      </section>

      <section className="landing-stats" aria-label="Показатели платформы">
        <StatTile label="Пользователи" value="390+" hint="готовая модель витрины и кабинета" />
        <StatTile label="Локации" value="20+" hint="серверы и панели управляются централизованно" />
        <StatTile label="Серверы" value="50+" hint="подготовка VPS и 3x-ui через админку" />
      </section>

      {profile && (
        <section className="section">
          <Card className="account-highlight">
            <div>
              <p className="eyebrow">Текущий аккаунт</p>
              <h3>{profile.displayName}</h3>
              <p>{profile.email ?? 'email не указан'}</p>
              <p>Реферальный код: <strong>{profile.referralCode}</strong></p>
            </div>
            <StatusBadge value={profile.status} />
          </Card>
        </section>
      )}

      <section className="landing-section landing-feature-section" id="features">
        <div className="landing-illustration" aria-hidden="true">
          <div className="device-card device-card-primary">
            <strong>WireGuard / VLESS</strong>
            <span>подключение в один клик</span>
          </div>
          <div className="device-card device-card-secondary">
            <strong>Telegram Bot</strong>
            <span>покупка, продление, поддержка</span>
          </div>
          <div className="device-card device-card-tertiary">
            <strong>Admin</strong>
            <span>тарифы, платежи, серверы</span>
          </div>
        </div>
        <div className="landing-section-copy">
          <p className="eyebrow">Возможности</p>
          <h2>{content('home.features.title')}</h2>
          <p>{content('home.features.subtitle')}</p>
          <ul className="feature-check-list">
            {featureItems.map((feature) => <li key={feature}>{feature}</li>)}
          </ul>
        </div>
      </section>

      <section className="landing-section landing-pricing-preview" id="pricing">
        <div className="landing-section-heading">
          <p className="eyebrow">Тарифы</p>
          <h2>{content('home.pricing.title')}</h2>
          <p>Реальные цены и доступные способы оплаты подтягиваются из API на странице тарифов.</p>
        </div>
        <div className="plan-grid">
          {landingPlans.map((plan, index) => (
            <Card key={plan.name} className={index === 1 ? 'plan-card plan-card-featured' : 'plan-card'}>
              <div className="plan-icon">{plan.name.slice(0, 1)}</div>
              <h3>{plan.name}</h3>
              <p className="muted">{plan.description}</p>
              <ul className="feature-check-list compact">
                {plan.features.map((feature) => <li key={feature}>{feature}</li>)}
              </ul>
              <p className="plan-price">{plan.price}<span>/ месяц</span></p>
              <Link to="/tariffs" className={index === 1 ? 'button' : 'button button-ghost'}>Смотреть тарифы</Link>
            </Card>
          ))}
        </div>
      </section>

      <section className="landing-section network-section">
        <div className="landing-section-heading">
          <p className="eyebrow">Сеть</p>
          <h2>{content('home.network.title')}</h2>
          <p>{content('home.network.subtitle')}</p>
        </div>
        <div className="coverage-map" aria-label="Карта покрытия VPN-сети">
          <span className="map-point point-eu">EU</span>
          <span className="map-point point-us">US</span>
          <span className="map-point point-tr">TR</span>
          <span className="map-point point-sg">SG</span>
          <span className="map-line line-a" />
          <span className="map-line line-b" />
          <span className="map-line line-c" />
        </div>
      </section>

      <section className="landing-section testimonials-section" id="testimonials">
        <div className="landing-section-heading">
          <p className="eyebrow">Отзывы</p>
          <h2>{content('home.testimonials.title')}</h2>
        </div>
        <div className="testimonial-grid">
          {testimonialItems.map((item) => (
            <Card key={item.name} className="testimonial-card">
              <div className="testimonial-head">
                <span className="avatar">{item.name.slice(0, 1)}</span>
                <div>
                  <strong>{item.name}</strong>
                  <small>{item.role}</small>
                </div>
              </div>
              <p>{item.text}</p>
            </Card>
          ))}
        </div>
      </section>

      <section className="landing-section faq-preview-section">
        <div className="landing-section-heading">
          <p className="eyebrow">FAQ</p>
          <h2>Коротко о покупке и подключении</h2>
          <p>Ответы управляются из админки и сразу обновляются на публичной странице.</p>
        </div>
        <div className="faq-preview-grid">
          {homeFaq.length === 0 && <EmptyState title="FAQ скоро появится" description="Администратор может добавить вопросы в разделе FAQ." />}
          {homeFaq.map((item) => (
            <Card key={item.id ?? item.question} className="faq-preview-card">
              <span>{item.category ?? 'Общее'}</span>
              <h3>{item.question}</h3>
              <p>{item.answer}</p>
            </Card>
          ))}
        </div>
        <Link to="/faq" className="button button-ghost">Открыть все вопросы</Link>
      </section>

      <section className="landing-cta">
        <div>
          <h2>{content('home.finalCta.title')}</h2>
          <p>{content('home.finalCta.subtitle')}</p>
        </div>
        <Link to="/tariffs" className="button">Перейти к тарифам</Link>
      </section>

      <footer className="landing-footer">
        <p>{content('home.footer.text')}</p>
        <span>{content('home.footer.support')}</span>
      </footer>
    </PageShell>
  )
}

function TariffsPage({ onPendingCheckout }: {
  onPendingCheckout: (pending: PendingCheckout) => void
}) {
  const [tariffs, setTariffs] = useState<TariffDto[]>([])
  const [tariffsLoading, setTariffsLoading] = useState(true)
  const [error, setError] = useState('')
  const [errorContentKey, setErrorContentKey] = useState('')
  const [promoCode, setPromoCode] = useState('')
  const [paymentProviders, setPaymentProviders] = useState<PublicPaymentProviderDto[]>([])
  const [paymentProvidersLoading, setPaymentProvidersLoading] = useState(true)
  const [provider, setProvider] = useState<PaymentProvider | ''>('')
  const [pendingTariffId, setPendingTariffId] = useState<string>('')
  const [pageContent, setPageContent] = useState<Record<string, string>>(defaultHomeContent)
  const checkoutInFlightRef = useRef(false)
  const checkoutRequestIdRef = useRef(0)
  const navigate = useNavigate()
  const content = (key: string) => pageContent[key] ?? defaultHomeContent[key] ?? ''
  const visibleError = errorContentKey ? content(errorContentKey) : error
  const checkoutUnavailableReason = getCheckoutUnavailableReason(paymentProvidersLoading, paymentProviders, provider, {
    loading: content('home.checkout.unavailable.loading'),
    noProviders: content('home.checkout.unavailable.noProviders'),
    chooseProvider: content('home.checkout.unavailable.chooseProvider')
  })
  const tariffsState = getPublicListState(tariffsLoading, visibleError, tariffs.length)

  useEffect(() => {
    let cancelled = false
    api.getHomeContent()
      .then((items) => {
        if (!cancelled) setPageContent({ ...defaultHomeContent, ...mapContent(items) })
      })
      .catch(() => {
        if (!cancelled) setPageContent(defaultHomeContent)
      })
    setTariffsLoading(true)
    api.getTariffs()
      .then((items) => {
        if (!cancelled) setTariffs(items)
      })
      .catch(() => {
        if (cancelled) return
        setError('')
        setErrorContentKey('home.errors.tariffsLoad')
      })
      .finally(() => {
        if (!cancelled) setTariffsLoading(false)
      })
    setPaymentProvidersLoading(true)
    api.getPublicPaymentProviders()
      .then((items) => {
        if (cancelled) return
        setPaymentProviders(items)
        setProvider((current) => current && items.some((item) => item.provider === current) ? current : (items[0]?.provider ?? ''))
      })
      .catch(() => {
        if (cancelled) return
        setPaymentProviders([])
        setProvider('')
        setError('')
        setErrorContentKey('home.errors.paymentProvidersLoad')
      })
      .finally(() => {
        if (!cancelled) setPaymentProvidersLoading(false)
      })

    return () => {
      cancelled = true
      checkoutRequestIdRef.current += 1
      checkoutInFlightRef.current = false
    }
  }, [])

  const handleCheckout = async (tariff: TariffDto) => {
    if (checkoutInFlightRef.current) return

    setError('')
    setErrorContentKey('')

    if (!provider) {
      setError(content('home.errors.noPaymentProviders'))
      return
    }

    checkoutInFlightRef.current = true
    const requestId = ++checkoutRequestIdRef.current
    setPendingTariffId(tariff.id)
    try {
      const session = await api.createCheckoutSession({
        tariffId: tariff.id,
        type: 'NewSubscription',
        paymentProvider: provider,
        promoCode: promoCode || null,
        emailHint: null,
        returnUrl: `${window.location.origin}/account`
      })
      if (requestId !== checkoutRequestIdRef.current) return

      const pending = { token: session.token, tariffName: tariff.name, provider }
      writeSessionStorageItem(PENDING_CHECKOUT_STORAGE_KEY, JSON.stringify(pending))
      onPendingCheckout(pending)
      navigate('/account')
    } catch (e) {
      if (requestId === checkoutRequestIdRef.current) {
        setError(getCheckoutErrorMessage(e, content('home.errors.checkoutCreate')))
      }
    } finally {
      if (requestId === checkoutRequestIdRef.current) {
        checkoutInFlightRef.current = false
        setPendingTariffId('')
      }
    }
  }

  return (
    <PageShell title="Тарифы и покупка">
      <div className="page-intro">
        <div>
          <h2 className="page-heading">Выберите тариф</h2>
          <p className="muted no-margin-bottom">Показываем только активные тарифы и доступные способы оплаты.</p>
        </div>
        <ValidationModeBadge label="Проверочный режим оплат" />
      </div>

      <div className="section">
        <Card>
          <h3>Настройки оформления</h3>
          <div className="toolbar">
            <label>
              <span>Способ оплаты</span>
              <select value={provider} disabled={paymentProvidersLoading || paymentProviders.length === 0 || Boolean(pendingTariffId)} onChange={(e) => setProvider(e.target.value as PaymentProvider)}>
                {paymentProviders.map((item) => (
                  <option key={item.provider} value={item.provider}>{item.publicName || item.provider}{item.mode === 'Sandbox' ? ' · проверка' : ''}</option>
                ))}
              </select>
            </label>
            <label>
              <span>Промокод</span>
              <input value={promoCode} disabled={Boolean(pendingTariffId)} onChange={(e) => setPromoCode(e.target.value)} placeholder="Например, WELCOME10" />
            </label>
          </div>
          {paymentProvidersLoading && <LoadingBlock label={content('home.checkout.unavailable.loading')} />}
          {!paymentProvidersLoading && paymentProviders.length === 0 && <EmptyState title={content('home.checkout.providersEmptyTitle')} description={content('home.checkout.providersEmptyDescription')} />}
          {!paymentProvidersLoading && paymentProviders.length > 0 && <p className="muted">Доступно способов оплаты: {paymentProviders.length}. Показываются только включенные и готовые web-провайдеры.</p>}
          <p className="muted">{content('home.checkout.settingsHint')}</p>
        </Card>
      </div>

      {visibleError && (
        <div className="section">
          <ErrorBlock message={visibleError} />
        </div>
      )}

      <div className="section card-list">
        {tariffsState === 'loading' && <LoadingBlock label="Загружаем тарифы..." />}
        {tariffsState === 'empty' && <EmptyState title="Тарифы пока не опубликованы" description="Администратор должен включить тариф для public/Telegram витрины." />}
        {tariffs.map((tariff) => {
          const features = tariffFeatures(tariff)
          const checkoutAvailable = canStartCheckout(pendingTariffId, paymentProvidersLoading, paymentProviders, provider)
          const buttonUnavailableReason = pendingTariffId
            ? 'Дождитесь завершения текущего оформления.'
            : checkoutUnavailableReason

          return (
          <div className="card" key={tariff.id}>
            <div className="card-head">
              <div>
                <h3>{tariff.name}</h3>
                <p>{tariff.description}</p>
              </div>
              <div className="status-stack">
                {tariff.badge && <StatusBadge value={tariff.badge} />}
                <StatusBadge value={tariff.category} />
              </div>
            </div>
            {tariff.fullDescription && <p>{tariff.fullDescription}</p>}
            <p><strong>{tariff.price} {tariff.currency}</strong> / {tariff.durationDays} дней</p>
            <p>Устройств: {tariff.maxDevices}{tariff.trafficLimit ? ` · трафик ${(tariff.trafficLimit / 1024 / 1024 / 1024).toFixed(0)} ГБ` : ''}</p>
            {features.length > 0 && (
              <ul className="feature-list compact-list">
                {features.map((feature) => <li key={feature}>{feature}</li>)}
              </ul>
            )}
            {tariff.afterPaymentText && <p className="muted">{tariff.afterPaymentText}</p>}
            <PrimaryButton disabled={!checkoutAvailable} aria-busy={pendingTariffId === tariff.id} title={buttonUnavailableReason || undefined} onClick={() => void handleCheckout(tariff)}>
              {pendingTariffId === tariff.id ? 'Создаем заказ...' : 'Купить'}
            </PrimaryButton>
            {checkoutUnavailableReason && <p className="muted">{checkoutUnavailableReason}</p>}
          </div>
          )
        })}
      </div>

    </PageShell>
  )
}

function FaqPage() {
  const [items, setItems] = useState<FaqItem[]>([])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState('')
  const [category, setCategory] = useState(FAQ_ALL_CATEGORY)

  const loadFaq = useCallback(() => {
    setLoading(true)
    setError('')
    api.getFaq()
      .then(setItems)
      .catch(() => setError('Не удалось загрузить FAQ. Проверьте подключение к API и попробуйте еще раз.'))
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    loadFaq()
  }, [loadFaq])

  const categories = useMemo(() => getFaqCategories(items), [items])
  const filteredItems = useMemo(() => filterFaqItems(items, category, search), [items, category, search])
  const filtersActive = search.trim().length > 0 || category !== FAQ_ALL_CATEGORY
  const emptyTitle = filtersActive ? 'Ничего не найдено' : 'FAQ пока пуст'
  const emptyDescription = filtersActive
    ? 'Измените поисковый запрос или выберите другую категорию.'
    : 'Администратор может добавить вопросы в разделе FAQ.'

  return (
    <PageShell title="FAQ">
      <div className="page-intro">
        <div>
          <h2 className="page-heading">Вопросы и ответы</h2>
          <p className="muted no-margin-bottom">Ответы загружаются из админки и обновляются без деплоя сайта.</p>
        </div>
        <StatusBadge value={loading ? 'Pending' : `${filteredItems.length} из ${items.length}`} />
      </div>
      {error && (
        <Card className="faq-error-card">
          <ErrorBlock message={error} />
          <PrimaryButton type="button" className="button-secondary" onClick={loadFaq} disabled={loading}>
            Повторить загрузку
          </PrimaryButton>
        </Card>
      )}
      <div className="faq-toolbar">
        <label>
          <span>Поиск</span>
          <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Оплата, подключение, продление" aria-label="Поиск по FAQ" />
        </label>
        <label>
          <span>Категория</span>
          <select value={category} onChange={(event) => setCategory(event.target.value)}>
            {categories.map((item) => <option key={item} value={item}>{item}</option>)}
          </select>
        </label>
      </div>
      {loading && <LoadingBlock label="Загружаем FAQ" />}
      <div className="card-list faq-list">
        {filteredItems.length === 0 && !error && !loading && <EmptyState title={emptyTitle} description={emptyDescription} />}
        {filteredItems.map((item, index) => (
          <details className="faq-item" key={item.id ?? item.question} open={filtersActive && index === 0 ? true : undefined}>
            <summary>
              <span>{item.question}</span>
              <small>{normalizeFaqCategory(item.category)}</small>
            </summary>
            <p>{item.answer}</p>
          </details>
        ))}
      </div>
    </PageShell>
  )
}

function AccountPage({
  token,
  profile,
  onAuthenticated,
  onPasswordReset,
  onLogout,
  logoutBusy,
  sessionError,
  sessionHydrationBusy,
  lastCheckout,
  pendingCheckout,
  pendingCheckoutOrder,
  checkoutError,
  claimBusy,
  onRetrySession,
  onRetryPendingCheckout,
  onClearPendingCheckout
}: {
  token: string
  profile: UserProfileDto | null
  onAuthenticated: (response: AuthResponse) => void
  onPasswordReset: () => void
  onLogout: () => Promise<void>
  logoutBusy: boolean
  sessionError: string
  sessionHydrationBusy: boolean
  lastCheckout: CheckoutState
  pendingCheckout: PendingCheckout | null
  pendingCheckoutOrder: OrderDto | null
  checkoutError: string
  claimBusy: boolean
  onRetrySession: () => void
  onRetryPendingCheckout: () => void
  onClearPendingCheckout: () => void
}) {
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [referralCode, setReferralCode] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [resetEmail, setResetEmail] = useState('')
  const [resetToken, setResetToken] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [resetMessage, setResetMessage] = useState('')
  const [pageContent, setPageContent] = useState<Record<string, string>>(defaultHomeContent)
  const accountActionInFlightRef = useRef<string | null>(null)
  const accountActionRequestIdRef = useRef(0)
  const resetEmailRef = useRef(resetEmail)
  const newPasswordRef = useRef(newPassword)
  resetEmailRef.current = resetEmail
  newPasswordRef.current = newPassword
  const content = (key: string) => pageContent[key] ?? defaultHomeContent[key] ?? ''

  useEffect(() => {
    api.getHomeContent()
      .then((items) => setPageContent({ ...defaultHomeContent, ...mapContent(items) }))
      .catch(() => setPageContent(defaultHomeContent))
  }, [])

  useEffect(() => () => {
    accountActionRequestIdRef.current += 1
    accountActionInFlightRef.current = null
  }, [])

  const submitLabel = mode === 'login' ? 'Войти' : 'Создать аккаунт'
  const authPanelId = 'public-auth-panel'
  const activeAuthTabId = mode === 'login' ? 'public-auth-login-tab' : 'public-auth-register-tab'
  const authValidationErrors = validateAuthInput(mode, email, password, displayName)
  const resetRequestErrors = validatePasswordResetRequest(resetEmail)
  const resetConfirmErrors = validatePasswordResetConfirm(resetToken, newPassword)
  const showAuthValidation = authValidationErrors.length > 0 && Boolean(email || password || displayName)
  const showResetRequestValidation = Boolean(resetEmail)
  const showResetConfirmValidation = Boolean(resetToken || newPassword)

  const switchAuthMode = (nextMode: 'login' | 'register') => {
    setMode(nextMode)
    setError('')
    setResetMessage('')
  }

  const runAccountAction = async (
    id: string,
    onError: (error: unknown) => void,
    action: (isCurrent: () => boolean) => Promise<void>
  ) => {
    if (accountActionInFlightRef.current) return
    accountActionInFlightRef.current = id
    const requestId = ++accountActionRequestIdRef.current
    const isCurrent = () => requestId === accountActionRequestIdRef.current
      && accountActionInFlightRef.current === id
    setBusy(true)
    setError('')
    try {
      await action(isCurrent)
    } catch (error) {
      if (isCurrent()) onError(error)
    } finally {
      if (isCurrent()) {
        accountActionInFlightRef.current = null
        setBusy(false)
      }
    }
  }

  const handleAuthSubmit = async () => {
    if (authValidationErrors.length > 0) {
      setError(authValidationErrors.join(' '))
      return
    }

    const submittedMode = mode
    const submittedEmail = email
    const submittedPassword = password
    const submittedDisplayName = displayName
    const submittedReferralCode = referralCode
    await runAccountAction(
      `auth-${submittedMode}`,
      (error) => setError(translateAuthError(error, 'Ошибка авторизации')),
      async (isCurrent) => {
        const response = submittedMode === 'login'
          ? await api.login(submittedEmail, submittedPassword)
          : await api.register(
            submittedEmail,
            submittedPassword,
            submittedDisplayName.trim() || submittedEmail.trim(),
            submittedReferralCode
          )
        if (isCurrent()) onAuthenticated(response)
      }
    )
  }

  const handleForgotPassword = async () => {
    if (resetRequestErrors.length > 0) {
      setError(resetRequestErrors.join(' '))
      return
    }

    const submittedEmail = resetEmail
    await runAccountAction(
      'forgot-password',
      (error) => setError(translateAuthError(error, 'Не удалось запросить сброс пароля')),
      async (isCurrent) => {
        const response = await api.forgotPassword(submittedEmail)
        if (!isCurrent()) return
        if (response.validationResetToken && resetEmailRef.current === submittedEmail) {
          setResetToken(response.validationResetToken)
        }
        setResetMessage(translateAuthMessage(response.message))
      }
    )
  }

  const handleResetPassword = async () => {
    if (resetConfirmErrors.length > 0) {
      setError(resetConfirmErrors.join(' '))
      return
    }

    const submittedToken = resetToken
    const submittedPassword = newPassword
    await runAccountAction(
      'reset-password',
      (error) => setError(translateAuthError(error, 'Не удалось изменить пароль')),
      async (isCurrent) => {
        await api.resetPassword(submittedToken, submittedPassword)
        if (!isCurrent()) return
        onPasswordReset()
        if (newPasswordRef.current === submittedPassword) setNewPassword('')
        setResetMessage('Пароль изменён. Войдите с новым паролем.')
      }
    )
  }

  return (
    <PageShell title="Аккаунт">
      <h2 className="sr-only">Состояние аккаунта</h2>
      <div className="grid">
        <StatTile label="Авторизация" value={profile ? 'подключен' : token ? (sessionHydrationBusy ? 'проверяется' : 'требует проверки') : 'не выполнена'} />
        <StatTile label="Покупка" value={pendingCheckoutOrder ? 'заказ создан' : pendingCheckout ? 'ожидает привязки' : lastCheckout ? 'есть заказ' : 'пока пусто'} />
        <StatTile label="Рефералы" value={profile?.referralCode ?? 'будет после входа'} />
      </div>

      {sessionError && <div className="section"><ErrorBlock message={sessionError} /></div>}

      {token && !profile && (
        <div className="section">
          <Card>
            <h3>Проверка сессии</h3>
            {sessionHydrationBusy
              ? <LoadingBlock label="Проверяем доступ к аккаунту..." />
              : <p className="muted">Токены сохранены локально, но доступ к профилю пока не подтверждён.</p>}
            <div className="form-actions">
              {!sessionHydrationBusy && <PrimaryButton type="button" onClick={onRetrySession}>Повторить проверку</PrimaryButton>}
              <PrimaryButton type="button" className="button-ghost" disabled={logoutBusy} aria-busy={logoutBusy} onClick={() => void onLogout()}>{logoutBusy ? 'Завершаем сессию...' : 'Завершить сессию'}</PrimaryButton>
            </div>
          </Card>
        </div>
      )}

      {checkoutError && (
        <div className="section">
          <Card>
            <ErrorBlock message={checkoutError} />
            {pendingCheckout && profile && (
              <div className="form-actions">
                <PrimaryButton type="button" disabled={claimBusy} aria-busy={claimBusy} onClick={onRetryPendingCheckout}>{pendingCheckoutOrder ? 'Повторить оплату' : 'Повторить привязку'}</PrimaryButton>
                <PrimaryButton type="button" className="button-ghost" disabled={claimBusy} onClick={onClearPendingCheckout}>Отменить эту покупку</PrimaryButton>
              </div>
            )}
          </Card>
        </div>
      )}

      {profile ? (
        <div className="section card-list">
          <Card>
            <h3>Профиль</h3>
            <p><strong>{profile.displayName}</strong></p>
            <p>{profile.email ?? 'email не указан'}</p>
            <p>Язык: {profile.preferredLanguage}</p>
            <p>Реферальный код: <strong>{profile.referralCode}</strong></p>
            <StatusBadge value={profile.status} />
            <div className="mt-12">
              <PrimaryButton className="button-secondary" disabled={logoutBusy} aria-busy={logoutBusy} onClick={() => void onLogout()}>{logoutBusy ? 'Завершаем сессию...' : 'Выйти'}</PrimaryButton>
            </div>
          </Card>

          {pendingCheckout && (
            <Card>
              <h3>{claimBusy ? (pendingCheckoutOrder ? 'Готовим оплату' : 'Привязываем покупку') : pendingCheckoutOrder ? 'Заказ создан, оплата не подготовлена' : 'Покупка ожидает привязки'}</h3>
              <p>Тариф: {pendingCheckout.tariffName}</p>
              <p>Способ оплаты: {pendingCheckout.provider}</p>
              {pendingCheckoutOrder && <p>ID заказа: {pendingCheckoutOrder.id}</p>}
              {claimBusy
                ? <LoadingBlock label={pendingCheckoutOrder ? 'Повторно готовим ссылку оплаты...' : 'Создаём заказ и готовим оплату...'} />
                : <p className="muted">{pendingCheckoutOrder ? 'Заказ сохранён. Повторная команда подготовит оплату для этого же заказа.' : 'Покупка будет привязана к текущему аккаунту, затем появится ссылка на оплату.'}</p>}
              {!claimBusy && (
                <div className="form-actions">
                  <PrimaryButton type="button" onClick={onRetryPendingCheckout}>{pendingCheckoutOrder ? 'Повторить оплату' : 'Привязать покупку'}</PrimaryButton>
                  <PrimaryButton type="button" className="button-ghost" onClick={onClearPendingCheckout}>Отменить</PrimaryButton>
                </div>
              )}
            </Card>
          )}

          {lastCheckout && (
            <Card>
              <h3>{content('home.checkout.resultTitle')}</h3>
              <p>Тариф: {lastCheckout.tariffName}</p>
              <p>Способ оплаты: {lastCheckout.provider}</p>
              <p>Статус: <StatusBadge value={lastCheckout.order.status} /></p>
              <p>ID заказа: {lastCheckout.order.id}</p>
              <p>ID платежа: {lastCheckout.payment.paymentId}</p>
              <p className="muted">{content('home.checkout.afterPaymentText')}</p>
              <ExternalLinkActions
                value={lastCheckout.payment.redirectUrl}
                openLabel={content('home.checkout.openPaymentCta')}
                copyLabel={content('home.checkout.copyPaymentLink')}
                ariaLabel="Открыть оплату в новой вкладке"
                invalidMessage="Ссылка оплаты отклонена как некорректная. Повторите оформление или обратитесь в поддержку."
              />
            </Card>
          )}
        </div>
      ) : token ? null : (
        <div className="section">
          {pendingCheckout && (
            <Card>
              <h3>Покупка сохранена</h3>
              <p>Тариф: {pendingCheckout.tariffName}</p>
              <p>Способ оплаты: {pendingCheckout.provider}</p>
              <p className="muted">Войдите или создайте аккаунт ниже. После входа заказ привяжется автоматически, и появится ссылка на оплату.</p>
              <div className="form-actions">
                <PrimaryButton type="button" className="button-ghost" onClick={onClearPendingCheckout}>Отменить эту покупку</PrimaryButton>
              </div>
            </Card>
          )}
          <Card>
            <div className="section-header">
              <div>
                <h3>{mode === 'login' ? 'Вход' : 'Регистрация'}</h3>
                {pendingCheckout && <p className="muted">После входа покупка будет привязана к вашему аккаунту автоматически.</p>}
              </div>
              <SegmentedTabs
                idPrefix="public-auth"
                panelId={authPanelId}
                label="Режим авторизации"
                value={mode}
                onChange={switchAuthMode}
                options={[
                  { value: 'login', label: 'Вход' },
                  { value: 'register', label: 'Регистрация' }
                ]}
              />
            </div>
            <div id={authPanelId} role="tabpanel" aria-labelledby={activeAuthTabId}>
              <form
                aria-busy={busy}
                onSubmit={(e) => {
                  e.preventDefault()
                  void handleAuthSubmit()
                }}
              >
                {mode === 'register' && <label><span>Имя</span><input value={displayName} onChange={(e) => setDisplayName(e.target.value)} placeholder="Как к вам обращаться" autoComplete="name" /></label>}
                {mode === 'register' && <label><span>Реферальный код</span><input value={referralCode} onChange={(e) => setReferralCode(e.target.value)} placeholder="Необязательно" autoComplete="off" /><small>Если вас пригласил другой пользователь, укажите его код.</small></label>}
                <label><span>Email</span><input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="you@example.com" type="email" autoComplete="email" required /><small>Используется для входа и привязки покупок.</small></label>
                <PasswordField label="Пароль" value={password} onChange={setPassword} placeholder="Минимум 8 символов" autoComplete={mode === 'login' ? 'current-password' : 'new-password'} minLength={8} required help="Минимум 8 символов." />
                {showAuthValidation && (
                  <ul className="validation-list" aria-live="polite">
                    {authValidationErrors.map((item) => <li key={item}>{item}</li>)}
                  </ul>
                )}
                <div className="form-actions">
                  <PrimaryButton type="submit" disabled={busy || authValidationErrors.length > 0} aria-busy={busy}>{busy ? 'Сохраняем...' : submitLabel}</PrimaryButton>
                </div>
              </form>
            </div>
            {busy && <LoadingBlock label="Обрабатываем запрос..." />}
            {error && <ErrorBlock message={error} />}
            {resetMessage && <p className="toast-success" role="status" aria-live="polite">{resetMessage}</p>}
          </Card>
          <Card>
            <h3>Сброс пароля</h3>
            <p className="muted">Код восстановления придёт на email. В локальном режиме проверки он может быть показан сразу.</p>
            <form aria-label="Запрос кода сброса" className="form-grid" aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleForgotPassword() }}>
              <label><span>Email</span><input value={resetEmail} onChange={(e) => setResetEmail(e.target.value)} placeholder="you@example.com" type="email" autoComplete="email" required /></label>
              {showResetRequestValidation && resetRequestErrors.length > 0 && (
                <ul className="validation-list" aria-live="polite">
                  {resetRequestErrors.map((item) => <li key={item}>{item}</li>)}
                </ul>
              )}
              <div className="form-actions">
                <PrimaryButton type="submit" className="button-ghost" disabled={resetRequestErrors.length > 0 || busy} aria-busy={busy}>Запросить код</PrimaryButton>
              </div>
            </form>
            <form aria-label="Подтверждение сброса пароля" className="form-grid mt-12" aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleResetPassword() }}>
              <PasswordField label="Код сброса" value={resetToken} onChange={setResetToken} placeholder="Одноразовый код" autoComplete="one-time-code" />
              <PasswordField label="Новый пароль" value={newPassword} onChange={setNewPassword} placeholder="Новый пароль" autoComplete="new-password" minLength={8} />
              {showResetConfirmValidation && resetConfirmErrors.length > 0 && (
                <ul className="validation-list" aria-live="polite">
                  {resetConfirmErrors.map((item) => <li key={item}>{item}</li>)}
                </ul>
              )}
              <div className="form-actions">
                <PrimaryButton type="submit" disabled={resetConfirmErrors.length > 0 || busy} aria-busy={busy}>Изменить пароль</PrimaryButton>
              </div>
            </form>
          </Card>
        </div>
      )}
    </PageShell>
  )
}

function UserHelpPage() {
  const steps = [
    ['1', 'Выберите тариф', 'Откройте страницу тарифов, сравните срок, цену и количество устройств.'],
    ['2', 'Оплатите заказ', 'Выберите доступный способ оплаты. Если вы еще не вошли, сайт предложит авторизоваться.'],
    ['3', 'Вернитесь в кабинет', 'После оплаты обновите кабинет: подписка, ссылка подключения и QR-код появятся после подтверждения платежа.'],
    ['4', 'Подключите устройство', 'Скопируйте ссылку или откройте QR-код в VPN-клиенте с поддержкой VLESS, VMess или Trojan.']
  ]

  return (
    <PageShell title="Помощь">
      <section className="landing-section help-hero">
        <p className="eyebrow">Помощь пользователю</p>
        <h2>Как купить и подключить VPN</h2>
        <p className="muted">Короткий путь от выбора тарифа до готовой ссылки подключения. Все покупки, платежи, подписки и обращения в поддержку сохраняются в личном кабинете.</p>
        <div className="hero-actions">
          <Link to="/tariffs" className="button">Выбрать тариф</Link>
          <Link to="/account" className="button button-ghost">Открыть кабинет</Link>
        </div>
      </section>

      <section className="landing-section help-steps" aria-label="Шаги покупки и подключения">
        {steps.map(([number, title, description]) => (
          <Card key={number} className="help-step-card">
            <span className="help-step-number">{number}</span>
            <h3>{title}</h3>
            <p>{description}</p>
          </Card>
        ))}
      </section>

      <section className="landing-section help-grid">
        <Card>
          <h3>Оплата</h3>
          <p>Показываются только включенные и готовые web-способы оплаты. Если провайдеров нет, покупка временно недоступна.</p>
          <p className="muted">После оплаты вернитесь в кабинет и нажмите обновление данных, если статус еще не изменился.</p>
        </Card>
        <Card>
          <h3>Подключение</h3>
          <p>Скопируйте ссылку подключения или используйте QR-код. Не передавайте ключ другим людям.</p>
          <p className="muted">Если клиент не принимает ссылку, проверьте, что она скопирована полностью.</p>
        </Card>
        <Card>
          <h3>Продление</h3>
          <p>В кабинете выберите активную подписку и нажмите "Продлить". После оплаты срок действия будет обновлен.</p>
        </Card>
        <Card>
          <h3>Поддержка</h3>
          <p>Создайте обращение в кабинете, если платеж завис, ссылка не появилась или VPN-клиент не подключается.</p>
        </Card>
      </section>
    </PageShell>
  )
}

function NotFoundPage() {
  return (
    <PageShell title="Страница не найдена">
      <div className="section">
        <EmptyState
          title="Ошибка 404"
          description="Проверьте адрес страницы или выберите доступный раздел."
          action={(
            <div className="not-found-actions">
              <Link className="button" to="/">На главную</Link>
              <Link className="button button-ghost" to="/help">Открыть помощь</Link>
            </div>
          )}
        />
      </div>
    </PageShell>
  )
}

export function App() {
  const [token, setToken] = useState(readSessionStorageItem(TOKEN_STORAGE_KEY) ?? '')
  const [refreshToken, setRefreshToken] = useState(readSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY) ?? '')
  const [profile, setProfile] = useState<UserProfileDto | null>(null)
  const [lastCheckout, setLastCheckout] = useState<CheckoutState>(null)
  const [pendingCheckout, setPendingCheckout] = useState<PendingCheckout | null>(readPendingCheckout())
  const [pendingCheckoutOrder, setPendingCheckoutOrder] = useState<OrderDto | null>(null)
  const [checkoutError, setCheckoutError] = useState('')
  const [claimBusy, setClaimBusy] = useState(false)
  const [claimAttempt, setClaimAttempt] = useState(0)
  const checkoutClaimInFlightRef = useRef(false)
  const checkoutClaimAttemptKeyRef = useRef('')
  const checkoutClaimRequestIdRef = useRef(0)
  const [logoutBusy, setLogoutBusy] = useState(false)
  const [sessionError, setSessionError] = useState('')
  const [sessionHydrationBusy, setSessionHydrationBusy] = useState(false)
  const [sessionHydrationAttempt, setSessionHydrationAttempt] = useState(0)
  const sessionHydrationInFlightRef = useRef(false)
  const sessionHydrationAttemptKeyRef = useRef('')
  const sessionHydrationRequestIdRef = useRef(0)
  const logoutInFlightRef = useRef(false)

  const invalidateSessionHydration = () => {
    sessionHydrationRequestIdRef.current += 1
    sessionHydrationInFlightRef.current = false
    setSessionHydrationBusy(false)
  }

  const clearSession = () => {
    invalidateSessionHydration()
    sessionHydrationAttemptKeyRef.current = ''
    checkoutClaimRequestIdRef.current += 1
    checkoutClaimInFlightRef.current = false
    checkoutClaimAttemptKeyRef.current = ''
    setClaimBusy(false)
    removeSessionStorageItem(TOKEN_STORAGE_KEY)
    removeSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY)
    setToken('')
    setRefreshToken('')
    setProfile(null)
    setLastCheckout(null)
    setPendingCheckoutOrder(null)
    setCheckoutError('')
  }

  useEffect(() => {
    if (!token) {
      setProfile(null)
      setSessionHydrationBusy(false)
      return
    }
    if (profile || sessionHydrationInFlightRef.current) return

    const attemptKey = `${token}:${refreshToken}:${sessionHydrationAttempt}`
    if (sessionHydrationAttemptKeyRef.current === attemptKey) return
    sessionHydrationAttemptKeyRef.current = attemptKey
    sessionHydrationInFlightRef.current = true
    const requestId = ++sessionHydrationRequestIdRef.current
    setSessionHydrationBusy(true)
    setSessionError('')

    void (async () => {
      try {
        try {
          const nextProfile = await api.getMe(token)
          if (requestId === sessionHydrationRequestIdRef.current) setProfile(nextProfile)
          return
        } catch (error) {
          if (requestId !== sessionHydrationRequestIdRef.current) return
          if (!isPublicAccessTokenExpired(error)) {
            if (isPublicSessionRejected(error)) {
              clearSession()
              setSessionError(publicSessionEndedMessage)
            } else {
              setSessionError(getPublicSessionCheckError(error))
            }
            return
          }
        }

        if (!refreshToken) {
          clearSession()
          setSessionError(publicSessionEndedMessage)
          return
        }

        const response = await api.refresh(refreshToken)
        if (requestId !== sessionHydrationRequestIdRef.current) return
        writeSessionStorageItem(TOKEN_STORAGE_KEY, response.accessToken)
        writeSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY, response.refreshToken)
        setToken(response.accessToken)
        setRefreshToken(response.refreshToken)
        const nextProfile = await api.getMe(response.accessToken)
        if (requestId !== sessionHydrationRequestIdRef.current) return
        setProfile(nextProfile)
      } catch (error) {
        if (requestId !== sessionHydrationRequestIdRef.current) return
        if (isPublicSessionRejected(error)) {
          clearSession()
          setSessionError(publicSessionEndedMessage)
        } else {
          setSessionError(getPublicSessionCheckError(error))
        }
      } finally {
        if (requestId === sessionHydrationRequestIdRef.current) {
          sessionHydrationInFlightRef.current = false
          setSessionHydrationBusy(false)
        }
      }
    })()
  }, [token, refreshToken, profile, sessionHydrationAttempt])

  const retrySessionHydration = () => {
    setSessionError('')
    setSessionHydrationAttempt((current) => current + 1)
  }

  const retryPendingCheckout = () => {
    setCheckoutError('')
    setClaimAttempt((current) => current + 1)
  }

  const clearPendingCheckout = () => {
    checkoutClaimRequestIdRef.current += 1
    checkoutClaimInFlightRef.current = false
    checkoutClaimAttemptKeyRef.current = ''
    setClaimBusy(false)
    setPendingCheckout(null)
    setPendingCheckoutOrder(null)
    setCheckoutError('')
    removeSessionStorageItem(PENDING_CHECKOUT_STORAGE_KEY)
  }

  useEffect(() => {
    if (!token || !profile || !pendingCheckout || checkoutClaimInFlightRef.current) return
    const attemptKey = `${token}:${pendingCheckout.token}:${claimAttempt}`
    if (checkoutClaimAttemptKeyRef.current === attemptKey) return

    checkoutClaimAttemptKeyRef.current = attemptKey
    checkoutClaimInFlightRef.current = true
    const requestId = ++checkoutClaimRequestIdRef.current
    setClaimBusy(true)
    setCheckoutError('')
    void (async () => {
      let order = pendingCheckoutOrder
      try {
        if (!order) {
          order = await api.claimCheckoutSession(token, pendingCheckout.token)
          if (requestId !== checkoutClaimRequestIdRef.current) return
          setPendingCheckoutOrder(order)
        }
        const payment = await api.initMyPayment(token, order.id, pendingCheckout.provider, `${window.location.origin}/account`)
        if (requestId !== checkoutClaimRequestIdRef.current) return
        const completed = { tariffName: pendingCheckout.tariffName, provider: pendingCheckout.provider, order, payment }
        setLastCheckout(completed)
        setPendingCheckout(null)
        setPendingCheckoutOrder(null)
        removeSessionStorageItem(PENDING_CHECKOUT_STORAGE_KEY)
      } catch (e) {
        if (requestId !== checkoutClaimRequestIdRef.current) return
        const fallback = order
          ? `Заказ ${order.id} создан, но ссылку оплаты подготовить не удалось.`
          : 'Не удалось привязать покупку.'
        setCheckoutError(getCheckoutErrorMessage(e, fallback))
      } finally {
        if (requestId === checkoutClaimRequestIdRef.current) {
          checkoutClaimInFlightRef.current = false
          setClaimBusy(false)
        }
      }
    })()
  }, [token, profile, pendingCheckout, pendingCheckoutOrder, claimAttempt])

  const handlePendingCheckout = (pending: PendingCheckout) => {
    checkoutClaimRequestIdRef.current += 1
    checkoutClaimInFlightRef.current = false
    checkoutClaimAttemptKeyRef.current = ''
    setClaimBusy(false)
    setPendingCheckoutOrder(null)
    setCheckoutError('')
    setPendingCheckout(pending)
  }

  const navigationLabel = useMemo(() => profile ? `Привет, ${profile.displayName}` : 'Аккаунт', [profile])

  const handleAuthenticated = (response: AuthResponse) => {
    invalidateSessionHydration()
    sessionHydrationAttemptKeyRef.current = ''
    writeSessionStorageItem(TOKEN_STORAGE_KEY, response.accessToken)
    writeSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY, response.refreshToken)
    setToken(response.accessToken)
    setRefreshToken(response.refreshToken)
    setProfile(null)
    setSessionError('')
  }

  const handleLogout = async () => {
    if (logoutInFlightRef.current) return
    logoutInFlightRef.current = true
    setLogoutBusy(true)
    setSessionError('')
    try {
      await api.logout(token || null, refreshToken || null)
    } catch {
      setSessionError('Локальная сессия завершена, но отзыв серверной сессии не подтверждён. На чужом устройстве измените пароль из доверенного браузера.')
    } finally {
      clearSession()
      logoutInFlightRef.current = false
      setLogoutBusy(false)
    }
  }

  return (
    <>
      <PublicRouteEffects />
      <SkipLink />
      <header className="topbar">
        <Link className="app-brand" to="/">VPN Platform</Link>
        <nav aria-label="Основная навигация">
          <NavLink to="/">Главная</NavLink>
          <NavLink to="/tariffs">Тарифы</NavLink>
          <NavLink to="/help">Помощь</NavLink>
          <NavLink to="/faq">FAQ</NavLink>
          <NavLink to="/account">{navigationLabel}</NavLink>
        </nav>
      </header>

      <Routes>
        <Route path="/" element={<LandingHomePage profile={profile} />} />
        <Route
          path="/tariffs"
          element={(
            <TariffsPage
              onPendingCheckout={handlePendingCheckout}
            />
          )}
        />
        <Route path="/help" element={<UserHelpPage />} />
        <Route path="/faq" element={<FaqPage />} />
        <Route
          path="/account"
          element={(
            <AccountPage
              token={token}
              profile={profile}
              onAuthenticated={handleAuthenticated}
              onPasswordReset={clearSession}
              onLogout={handleLogout}
              logoutBusy={logoutBusy}
              sessionError={sessionError}
              sessionHydrationBusy={sessionHydrationBusy}
              lastCheckout={lastCheckout}
              pendingCheckout={pendingCheckout}
              pendingCheckoutOrder={pendingCheckoutOrder}
              checkoutError={checkoutError}
              claimBusy={claimBusy}
              onRetrySession={retrySessionHydration}
              onRetryPendingCheckout={retryPendingCheckout}
              onClearPendingCheckout={clearPendingCheckout}
            />
          )}
        />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </>
  )
}
