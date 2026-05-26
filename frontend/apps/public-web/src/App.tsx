import React, { useEffect, useMemo, useState } from 'react'
import { Link, NavLink, Route, Routes, useNavigate } from 'react-router-dom'
import {
  ApiClient,
  AuthResponse,
  FaqItem,
  OrderDto,
  PaymentInitResult,
  PaymentProvider,
  PublicPaymentProviderDto,
  TariffDto,
  UserProfileDto
} from '@vpn-platform/api-client'
import { Card, CodeBlock, CopyButton, EmptyState, ErrorBlock, LoadingBlock, PageShell, PasswordField, PrimaryButton, SkipLink, StatTile, StatusBadge, ValidationModeBadge } from '@vpn-platform/ui'

const api = new ApiClient(import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080')
const TOKEN_STORAGE_KEY = 'vpn-platform-public-token'
const PENDING_CHECKOUT_STORAGE_KEY = 'vpn-platform-pending-checkout'

type CheckoutState = {
  tariffName: string
  provider: PaymentProvider
  order: OrderDto
  payment: PaymentInitResult
} | null

type PendingCheckout = {
  token: string
  tariffName: string
  provider: PaymentProvider
}

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

function readPendingCheckout(): PendingCheckout | null {
  const raw = readSessionStorageItem(PENDING_CHECKOUT_STORAGE_KEY)
  if (!raw) return null

  try {
    return JSON.parse(raw) as PendingCheckout
  } catch {
    removeSessionStorageItem(PENDING_CHECKOUT_STORAGE_KEY)
    return null
  }
}

function HomePage({ profile }: { profile: UserProfileDto | null }) {
  return (
    <PageShell title="VPN Platform">
      <div className="hero">
        <h2>VPN-доступ с автоматической выдачей</h2>
        <p>Выберите тариф, оплатите удобным способом и получите готовую ссылку для подключения после подтверждения оплаты. Все рискованные операции сначала проходят в безопасном режиме проверки.</p>
        <div className="hero-actions">
          <Link to="/tariffs" className="button">Выбрать тариф</Link>
          <Link to="/account" className="button button-secondary">{profile ? 'Мой аккаунт' : 'Войти / зарегистрироваться'}</Link>
        </div>
      </div>

      <div className="grid">
        <StatTile label="Покупка" value="заказ" hint="заказ создаётся безопасно" />
        <StatTile label="Оплата" value="проверена" hint="показываются только доступные способы" />
        <StatTile label="Выдача" value="VPN-ссылка" hint="после подтверждения платежа" />
      </div>

      {profile && (
        <div className="section">
          <Card>
            <h3>Текущий аккаунт</h3>
            <p><strong>{profile.displayName}</strong></p>
            <p>{profile.email ?? 'email не указан'}</p>
            <p>Реферальный код: <strong>{profile.referralCode}</strong></p>
            <StatusBadge value={profile.status} />
          </Card>
        </div>
      )}

      <div className="section card-list">
        <Card>
          <h3>Безопасная покупка</h3>
          <p>Покупка начинается без лишних технических шагов: пользователь выбирает тариф, входит в аккаунт и переходит к оплате. Недоступные способы оплаты не показываются.</p>
        </Card>
        <Card>
          <h3>Платежи</h3>
          <p>Пользователь видит только способы оплаты, которые сейчас готовы принимать платежи. Это снижает количество ошибок на этапе покупки.</p>
        </Card>
        <Card>
          <h3>Автовыдача</h3>
          <p>После подтверждения платежа подписка активируется автоматически, а готовая ссылка для подключения появляется в кабинете.</p>
        </Card>
      </div>
    </PageShell>
  )
}

function TariffsPage({ token, onCheckoutComplete, onPendingCheckout }: {
  token: string
  onCheckoutComplete: (state: CheckoutState) => void
  onPendingCheckout: (pending: PendingCheckout) => void
}) {
  const [tariffs, setTariffs] = useState<TariffDto[]>([])
  const [tariffsLoading, setTariffsLoading] = useState(true)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [promoCode, setPromoCode] = useState('')
  const [paymentProviders, setPaymentProviders] = useState<PublicPaymentProviderDto[]>([])
  const [paymentProvidersLoading, setPaymentProvidersLoading] = useState(true)
  const [provider, setProvider] = useState<PaymentProvider | ''>('')
  const [pendingTariffId, setPendingTariffId] = useState<string>('')
  const [checkoutState, setCheckoutState] = useState<CheckoutState>(null)
  const navigate = useNavigate()
  const checkoutUnavailableReason = paymentProvidersLoading
    ? 'Загружаем способы оплаты...'
    : paymentProviders.length === 0
      ? 'Оплата временно недоступна: нет включенных способов оплаты.'
      : !provider
        ? 'Выберите способ оплаты перед покупкой.'
        : ''

  useEffect(() => {
    setTariffsLoading(true)
    api.getTariffs().then(setTariffs).catch((e: Error) => setError(e.message)).finally(() => setTariffsLoading(false))
    setPaymentProvidersLoading(true)
    api.getPublicPaymentProviders()
      .then((items) => {
        setPaymentProviders(items)
        setProvider((current) => current && items.some((item) => item.provider === current) ? current : (items[0]?.provider ?? ''))
      })
      .catch((e: Error) => {
        setPaymentProviders([])
        setProvider('')
        setError(e.message)
      })
      .finally(() => setPaymentProvidersLoading(false))
  }, [])

  const handleCheckout = async (tariff: TariffDto) => {
    setError('')
    setNotice('')
    setPendingTariffId(tariff.id)

    if (!provider) {
      setError('Нет доступных платежных провайдеров. Попробуйте позже или обратитесь в поддержку.')
      setPendingTariffId('')
      return
    }

    try {
      const session = await api.createCheckoutSession({
        tariffId: tariff.id,
        type: 'NewSubscription',
        channel: 'Web',
        paymentProvider: provider,
        promoCode: promoCode || null,
        isFirstPurchase: false,
        emailHint: null,
        returnUrl: `${window.location.origin}/account`
      })

      const pending = { token: session.token, tariffName: tariff.name, provider }
      writeSessionStorageItem(PENDING_CHECKOUT_STORAGE_KEY, JSON.stringify(pending))
      onPendingCheckout(pending)

      if (!token) {
        setNotice('Покупка создана. Войдите или зарегистрируйтесь, чтобы привязать заказ и перейти к оплате.')
        navigate('/account')
        return
      }

      const order = await api.claimCheckoutSession(token, session.token)
      const payment = await api.initMyPayment(token, order.id, provider, `${window.location.origin}/account`)
      const nextState = { tariffName: tariff.name, provider, order, payment }
      removeSessionStorageItem(PENDING_CHECKOUT_STORAGE_KEY)
      setCheckoutState(nextState)
      onCheckoutComplete(nextState)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось создать покупку')
    } finally {
      setPendingTariffId('')
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
              <select value={provider} disabled={paymentProvidersLoading || paymentProviders.length === 0} onChange={(e) => setProvider(e.target.value as PaymentProvider)}>
                {paymentProviders.map((item) => (
                  <option key={item.provider} value={item.provider}>{item.publicName || item.provider}</option>
                ))}
              </select>
            </label>
            <label>
              <span>Промокод</span>
              <input value={promoCode} onChange={(e) => setPromoCode(e.target.value)} placeholder="Например, WELCOME10" />
            </label>
          </div>
          {paymentProvidersLoading && <LoadingBlock label="Загружаем доступные способы оплаты..." />}
          {!paymentProvidersLoading && paymentProviders.length === 0 && <EmptyState title="Нет доступных способов оплаты" description="Покупка временно недоступна: нет включенного и настроенного способа оплаты." />}
          <p className="muted">Если вы еще не вошли, мы сохраним выбранный тариф и попросим авторизоваться перед оплатой.</p>
        </Card>
      </div>

      {notice && (
        <div className="section">
          <Card>
            <p className="success-text">{notice}</p>
            <Link to="/account" className="button">Перейти к аккаунту</Link>
          </Card>
        </div>
      )}

      {error && (
        <div className="section">
          <ErrorBlock message={error} />
        </div>
      )}

      <div className="section card-list">
        {tariffsLoading && <LoadingBlock label="Загружаем тарифы..." />}
        {!tariffsLoading && tariffs.length === 0 && <EmptyState title="Тарифы пока не опубликованы" description="Администратор должен включить тариф для public/Telegram витрины." />}
        {tariffs.map((tariff) => (
          <div className="card" key={tariff.id}>
            <div className="card-head">
              <div>
                <h3>{tariff.name}</h3>
                <p>{tariff.description}</p>
              </div>
              <StatusBadge value={tariff.category} />
            </div>
            <p><strong>{tariff.price} {tariff.currency}</strong> / {tariff.durationDays} дней</p>
            <p>Устройств: {tariff.maxDevices}</p>
            <PrimaryButton disabled={pendingTariffId === tariff.id || paymentProvidersLoading || !provider || paymentProviders.length === 0} aria-busy={pendingTariffId === tariff.id} title={checkoutUnavailableReason || undefined} onClick={() => void handleCheckout(tariff)}>
              {pendingTariffId === tariff.id ? 'Создаем заказ...' : 'Купить'}
            </PrimaryButton>
            {checkoutUnavailableReason && <p className="muted">{checkoutUnavailableReason}</p>}
          </div>
        ))}
      </div>

      {checkoutState && (
        <div className="section">
          <Card>
            <h3>Последняя покупка</h3>
            <p>Тариф: <strong>{checkoutState.tariffName}</strong></p>
            <p>Способ оплаты: {checkoutState.provider}</p>
            <p>Заказ: <StatusBadge value={checkoutState.order.status} /></p>
            <p>ID заказа: {checkoutState.order.id}</p>
            <p>Платеж: {checkoutState.payment.paymentId}</p>
            <div className="copy-row">
              <a href={checkoutState.payment.redirectUrl} target="_blank" rel="noreferrer" className="button" aria-label="Открыть оплату в новой вкладке">
                Открыть оплату
              </a>
              <CopyButton value={checkoutState.payment.redirectUrl} label="Скопировать ссылку" />
            </div>
            <div className="mt-16">
              <CodeBlock>{checkoutState.payment.redirectUrl}</CodeBlock>
            </div>
          </Card>
        </div>
      )}
    </PageShell>
  )
}

function FaqPage() {
  const [items, setItems] = useState<FaqItem[]>([])
  const [error, setError] = useState('')

  useEffect(() => {
    api.getFaq().then(setItems).catch((e: Error) => setError(e.message))
  }, [])

  return (
    <PageShell title="FAQ">
      {error && <ErrorBlock message={error} />}
      <div className="card-list">
        {items.length === 0 && !error && <EmptyState title="FAQ пока пуст" description="Базовые инструкции доступны в Telegram-боте и личном кабинете." />}
        {items.map((item) => (
          <div className="card" key={item.question}>
            <h3>{item.question}</h3>
            <p>{item.answer}</p>
          </div>
        ))}
      </div>
    </PageShell>
  )
}

function AccountPage({
  token,
  profile,
  onAuthenticated,
  onLogout,
  lastCheckout,
  pendingCheckout,
  checkoutError,
  claimBusy,
  onRetryPendingCheckout,
  onClearPendingCheckout
}: {
  token: string
  profile: UserProfileDto | null
  onAuthenticated: (response: AuthResponse) => void
  onLogout: () => void
  lastCheckout: CheckoutState
  pendingCheckout: PendingCheckout | null
  checkoutError: string
  claimBusy: boolean
  onRetryPendingCheckout: () => void
  onClearPendingCheckout: () => void
}) {
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [resetEmail, setResetEmail] = useState('')
  const [resetToken, setResetToken] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [resetMessage, setResetMessage] = useState('')

  const submitLabel = mode === 'login' ? 'Войти' : 'Создать аккаунт'
  const authPanelId = 'public-auth-panel'
  const activeAuthTabId = mode === 'login' ? 'public-auth-login-tab' : 'public-auth-register-tab'

  const switchAuthMode = (nextMode: 'login' | 'register') => {
    setMode(nextMode)
    setError('')
    setResetMessage('')
  }

  const handleAuthTabsKeyDown = (event: React.KeyboardEvent<HTMLDivElement>) => {
    const modes: Array<'login' | 'register'> = ['login', 'register']
    const currentIndex = modes.indexOf(mode)
    const nextMode = event.key === 'ArrowRight' || event.key === 'ArrowDown'
      ? modes[(currentIndex + 1) % modes.length]
      : event.key === 'ArrowLeft' || event.key === 'ArrowUp'
        ? modes[(currentIndex + modes.length - 1) % modes.length]
        : event.key === 'Home'
          ? modes[0]
          : event.key === 'End'
            ? modes[modes.length - 1]
            : null

    if (!nextMode) return
    event.preventDefault()
    switchAuthMode(nextMode)
    const nextTabId = nextMode === 'login' ? 'public-auth-login-tab' : 'public-auth-register-tab'
    window.requestAnimationFrame(() => document.getElementById(nextTabId)?.focus())
  }

  const handleForgotPassword = async () => {
    setBusy(true)
    setError('')
    try {
      const response = await api.forgotPassword(resetEmail)
      if (response.validationResetToken) setResetToken(response.validationResetToken)
      setResetMessage(response.message)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось запросить сброс пароля')
    } finally {
      setBusy(false)
    }
  }

  const handleResetPassword = async () => {
    setBusy(true)
    setError('')
    try {
      await api.resetPassword(resetToken, newPassword)
      setNewPassword('')
      setResetMessage('Пароль изменён. Войдите с новым паролем.')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось изменить пароль')
    } finally {
      setBusy(false)
    }
  }

  return (
    <PageShell title="Аккаунт">
      <div className="grid">
        <StatTile label="Авторизация" value={token ? 'подключен' : 'не выполнена'} />
        <StatTile label="Покупка" value={pendingCheckout ? 'ожидает привязки' : lastCheckout ? 'есть заказ' : 'пока пусто'} />
        <StatTile label="Рефералы" value={profile?.referralCode ?? 'будет после входа'} />
      </div>

      {checkoutError && (
        <div className="section">
          <Card>
            <ErrorBlock message={checkoutError} />
            {pendingCheckout && token && (
              <div className="form-actions">
                <PrimaryButton type="button" disabled={claimBusy} aria-busy={claimBusy} onClick={onRetryPendingCheckout}>Повторить привязку</PrimaryButton>
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
              <PrimaryButton className="button-secondary" onClick={onLogout}>Выйти</PrimaryButton>
            </div>
          </Card>

          {pendingCheckout && (
            <Card>
              <h3>{claimBusy ? 'Привязываем покупку' : 'Покупка ожидает привязки'}</h3>
              <p>Тариф: {pendingCheckout.tariffName}</p>
              <p>Способ оплаты: {pendingCheckout.provider}</p>
              {claimBusy ? <LoadingBlock label="Создаём заказ и готовим оплату..." /> : <p className="muted">Покупка будет привязана к текущему аккаунту, затем появится ссылка на оплату.</p>}
              {!claimBusy && (
                <div className="form-actions">
                  <PrimaryButton type="button" onClick={onRetryPendingCheckout}>Привязать покупку</PrimaryButton>
                  <PrimaryButton type="button" className="button-ghost" onClick={onClearPendingCheckout}>Отменить</PrimaryButton>
                </div>
              )}
            </Card>
          )}

          {lastCheckout && (
            <Card>
              <h3>Последний заказ</h3>
              <p>Тариф: {lastCheckout.tariffName}</p>
              <p>Статус: <StatusBadge value={lastCheckout.order.status} /></p>
              <div className="copy-row">
                <a href={lastCheckout.payment.redirectUrl} target="_blank" rel="noreferrer" className="button" aria-label="Открыть оплату в новой вкладке">Открыть оплату</a>
                <CopyButton value={lastCheckout.payment.redirectUrl} label="Скопировать ссылку" />
              </div>
              <CodeBlock>{lastCheckout.payment.redirectUrl}</CodeBlock>
            </Card>
          )}
        </div>
      ) : (
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
              <div className="segmented-control" role="tablist" aria-label="Режим авторизации" aria-orientation="horizontal" onKeyDown={handleAuthTabsKeyDown}>
                <PrimaryButton id="public-auth-login-tab" type="button" role="tab" className={mode === 'login' ? 'active' : ''} aria-selected={mode === 'login'} aria-controls={authPanelId} tabIndex={mode === 'login' ? 0 : -1} onClick={() => switchAuthMode('login')}>Вход</PrimaryButton>
                <PrimaryButton id="public-auth-register-tab" type="button" role="tab" className={mode === 'register' ? 'active' : ''} aria-selected={mode === 'register'} aria-controls={authPanelId} tabIndex={mode === 'register' ? 0 : -1} onClick={() => switchAuthMode('register')}>Регистрация</PrimaryButton>
              </div>
            </div>
            <form
              id={authPanelId}
              role="tabpanel"
              aria-labelledby={activeAuthTabId}
              aria-busy={busy}
              onSubmit={async (e) => {
                e.preventDefault()
                setBusy(true)
                setError('')
                try {
                  const response = mode === 'login'
                    ? await api.login(email, password)
                    : await api.register(email, password, displayName)
                  onAuthenticated(response)
                } catch (e) {
                  setError(e instanceof Error ? e.message : 'Ошибка авторизации')
                } finally {
                  setBusy(false)
                }
              }}
            >
              {mode === 'register' && <label><span>Имя</span><input value={displayName} onChange={(e) => setDisplayName(e.target.value)} placeholder="Как к вам обращаться" autoComplete="name" /></label>}
              <label><span>Email</span><input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="you@example.com" type="email" autoComplete="email" required /><small>Используется для входа и привязки покупок.</small></label>
              <PasswordField label="Пароль" value={password} onChange={setPassword} placeholder="Минимум 8 символов" autoComplete={mode === 'login' ? 'current-password' : 'new-password'} minLength={8} required help="Минимум 8 символов." />
              <div className="form-actions">
                <PrimaryButton type="submit" disabled={busy || !email || !password} aria-busy={busy}>{busy ? 'Сохраняем...' : submitLabel}</PrimaryButton>
              </div>
            </form>
            {busy && <LoadingBlock label="Обрабатываем запрос..." />}
            {error && <ErrorBlock message={error} />}
            {resetMessage && <p className="toast-success" role="status" aria-live="polite">{resetMessage}</p>}
          </Card>
          <Card>
            <h3>Сброс пароля</h3>
            <p className="muted">В режиме проверки письмо не отправляется наружу: API возвращает одноразовый код только для локальной проверки сценария.</p>
            <form className="form-grid" aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleResetPassword() }}>
              <label><span>Email</span><input value={resetEmail} onChange={(e) => setResetEmail(e.target.value)} placeholder="you@example.com" type="email" autoComplete="email" required /></label>
              <PasswordField label="Код сброса" value={resetToken} onChange={setResetToken} placeholder="Одноразовый код" autoComplete="one-time-code" />
              <PasswordField label="Новый пароль" value={newPassword} onChange={setNewPassword} placeholder="Новый пароль" autoComplete="new-password" minLength={8} />
              <div className="form-actions">
                <PrimaryButton type="button" className="button-ghost" disabled={!resetEmail || busy} aria-busy={busy} onClick={() => void handleForgotPassword()}>Запросить код</PrimaryButton>
                <PrimaryButton type="submit" disabled={!resetToken || !newPassword || busy} aria-busy={busy}>Изменить пароль</PrimaryButton>
              </div>
            </form>
          </Card>
        </div>
      )}
    </PageShell>
  )
}

export function App() {
  const [token, setToken] = useState(readSessionStorageItem(TOKEN_STORAGE_KEY) ?? '')
  const [profile, setProfile] = useState<UserProfileDto | null>(null)
  const [lastCheckout, setLastCheckout] = useState<CheckoutState>(null)
  const [pendingCheckout, setPendingCheckout] = useState<PendingCheckout | null>(readPendingCheckout())
  const [checkoutError, setCheckoutError] = useState('')
  const [claimBusy, setClaimBusy] = useState(false)
  const [claimAttempt, setClaimAttempt] = useState(0)

  useEffect(() => {
    if (!token) {
      setProfile(null)
      return
    }

    api.getMe(token).then(setProfile).catch(() => {
      setToken('')
      removeSessionStorageItem(TOKEN_STORAGE_KEY)
    })
  }, [token])

  const retryPendingCheckout = () => {
    setCheckoutError('')
    setClaimAttempt((current) => current + 1)
  }

  const clearPendingCheckout = () => {
    setPendingCheckout(null)
    setCheckoutError('')
    removeSessionStorageItem(PENDING_CHECKOUT_STORAGE_KEY)
  }

  useEffect(() => {
    if (!token || !pendingCheckout || claimBusy) return

    setClaimBusy(true)
    setCheckoutError('')
    api.claimCheckoutSession(token, pendingCheckout.token)
      .then(async (order) => {
        const payment = await api.initMyPayment(token, order.id, pendingCheckout.provider, `${window.location.origin}/account`)
        const completed = { tariffName: pendingCheckout.tariffName, provider: pendingCheckout.provider, order, payment }
        setLastCheckout(completed)
        setPendingCheckout(null)
        removeSessionStorageItem(PENDING_CHECKOUT_STORAGE_KEY)
      })
      .catch((e: Error) => setCheckoutError(e.message))
      .finally(() => setClaimBusy(false))
  }, [token, pendingCheckout, claimAttempt])

  const navigationLabel = useMemo(() => profile ? `Привет, ${profile.displayName}` : 'Аккаунт', [profile])

  const handleAuthenticated = (response: AuthResponse) => {
    writeSessionStorageItem(TOKEN_STORAGE_KEY, response.accessToken)
    setToken(response.accessToken)
  }

  const handleLogout = () => {
    removeSessionStorageItem(TOKEN_STORAGE_KEY)
    setToken('')
    setProfile(null)
  }

  return (
    <>
      <SkipLink />
      <header className="topbar">
        <Link className="app-brand" to="/">VPN Platform</Link>
        <nav aria-label="Основная навигация">
          <NavLink to="/">Главная</NavLink>
          <NavLink to="/tariffs">Тарифы</NavLink>
          <NavLink to="/faq">FAQ</NavLink>
          <NavLink to="/account">{navigationLabel}</NavLink>
        </nav>
      </header>

      <Routes>
        <Route path="/" element={<HomePage profile={profile} />} />
        <Route
          path="/tariffs"
          element={(
            <TariffsPage
              token={token}
              onCheckoutComplete={setLastCheckout}
              onPendingCheckout={setPendingCheckout}
            />
          )}
        />
        <Route path="/faq" element={<FaqPage />} />
        <Route
          path="/account"
          element={(
            <AccountPage
              token={token}
              profile={profile}
              onAuthenticated={handleAuthenticated}
              onLogout={handleLogout}
              lastCheckout={lastCheckout}
              pendingCheckout={pendingCheckout}
              checkoutError={checkoutError}
              claimBusy={claimBusy}
              onRetryPendingCheckout={retryPendingCheckout}
              onClearPendingCheckout={clearPendingCheckout}
            />
          )}
        />
      </Routes>
    </>
  )
}
