import React, { useEffect, useMemo, useState } from 'react'
import {
  AccessCredentialDto,
  ApiClient,
  AuthResponse,
  OrderDto,
  PaymentAttemptDto,
  PaymentInitResult,
  PaymentProvider,
  PublicPaymentProviderDto,
  RewardLedgerDto,
  SubscriptionDto,
  TelegramLinkTokenDto,
  TelegramStatusDto,
  UserProfileDto
} from '@vpn-platform/api-client'
import { Card, CodeBlock, CopyButton, EmptyState, ErrorBlock, LoadingBlock, PageShell, PasswordField, PrimaryButton, SkipLink, StatTile, StatusBadge, ValidationModeBadge } from '@vpn-platform/ui'
import { AppVersionGate } from './AppVersion'

const api = new ApiClient(import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080')
const configuredPublicWebUrl = import.meta.env.VITE_PUBLIC_WEB_URL?.replace(/\/$/, '')
const TOKEN_STORAGE_KEY = 'vpn-platform-cabinet-token'
const REFRESH_TOKEN_STORAGE_KEY = 'vpn-platform-cabinet-refresh-token'

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

type RenewalState = {
  subscriptionId: string
  order: OrderDto
  payment: PaymentInitResult
} | null

export function App() {
  const [token, setToken] = useState(readSessionStorageItem(TOKEN_STORAGE_KEY) ?? '')
  const [refreshToken, setRefreshToken] = useState(readSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY) ?? '')
  const [profile, setProfile] = useState<UserProfileDto | null>(null)
  const [subscriptions, setSubscriptions] = useState<SubscriptionDto[]>([])
  const [orders, setOrders] = useState<OrderDto[]>([])
  const [payments, setPayments] = useState<PaymentAttemptDto[]>([])
  const [accesses, setAccesses] = useState<AccessCredentialDto[]>([])
  const [referrals, setReferrals] = useState<RewardLedgerDto[]>([])
  const [telegramStatus, setTelegramStatus] = useState<TelegramStatusDto | null>(null)
  const [telegramLink, setTelegramLink] = useState<TelegramLinkTokenDto | null>(null)
  const [paymentProviders, setPaymentProviders] = useState<PublicPaymentProviderDto[]>([])
  const [paymentProvidersLoading, setPaymentProvidersLoading] = useState(true)
  const [paymentProvidersError, setPaymentProvidersError] = useState('')
  const [provider, setProvider] = useState<PaymentProvider | ''>('')
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [busy, setBusy] = useState(false)
  const [authMode, setAuthMode] = useState<'login' | 'register'>('login')
  const [authEmail, setAuthEmail] = useState('')
  const [authPassword, setAuthPassword] = useState('')
  const [authDisplayName, setAuthDisplayName] = useState('')
  const [renewalState, setRenewalState] = useState<RenewalState>(null)
  const [qrSvgs, setQrSvgs] = useState<Record<string, string>>({})
  const [resetEmail, setResetEmail] = useState('')
  const [resetToken, setResetToken] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [appVersionOpenSignal, setAppVersionOpenSignal] = useState(0)
  const authPanelId = 'cabinet-auth-panel'
  const activeAuthTabId = authMode === 'login' ? 'cabinet-auth-login-tab' : 'cabinet-auth-register-tab'
  const publicWebUrl = useMemo(() => {
    if (configuredPublicWebUrl) return configuredPublicWebUrl
    if (typeof window === 'undefined') return 'http://localhost:5173'

    const devPortMap: Record<string, string> = {
      '5174': '5173',
      '5474': '5473'
    }
    const publicPort = devPortMap[window.location.port]

    return publicPort
      ? `${window.location.protocol}//${window.location.hostname}:${publicPort}`
      : window.location.origin
  }, [])

  const activeSubscriptions = useMemo(
    () => subscriptions.filter((item) => item.status === 'Active' || item.status === 'GracePeriod').length,
    [subscriptions]
  )

  const clearSession = () => {
    setToken('')
    setRefreshToken('')
    setProfile(null)
    setSubscriptions([])
    setOrders([])
    setPayments([])
    setAccesses([])
    setReferrals([])
    setTelegramStatus(null)
    setTelegramLink(null)
    setRenewalState(null)
    setQrSvgs({})
    removeSessionStorageItem(TOKEN_STORAGE_KEY)
    removeSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY)
  }

  const storeSession = async (response: AuthResponse) => {
    setToken(response.accessToken)
    setRefreshToken(response.refreshToken)
    writeSessionStorageItem(TOKEN_STORAGE_KEY, response.accessToken)
    writeSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY, response.refreshToken)
    await loadAll(response.accessToken)
  }

  const loadAll = async (currentToken: string) => {
    if (!currentToken) return

    setBusy(true)
    setError('')

    try {
      const [nextProfile, nextSubscriptions, nextOrders, nextPayments, nextAccesses, nextReferrals, nextTelegramStatus] = await Promise.all([
        api.getMe(currentToken),
        api.getMySubscriptions(currentToken),
        api.getMyOrders(currentToken),
        api.getMyPayments(currentToken),
        api.getMyAccesses(currentToken),
        api.getMyReferrals(currentToken),
        api.getTelegramStatus(currentToken)
      ])

      setProfile(nextProfile)
      setSubscriptions(nextSubscriptions)
      setOrders(nextOrders)
      setPayments(nextPayments)
      setAccesses(nextAccesses)
      setReferrals(nextReferrals)
      setTelegramStatus(nextTelegramStatus)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось загрузить кабинет')
    } finally {
      setBusy(false)
    }
  }

  useEffect(() => {
    void loadAll(token)
  }, [token])

  useEffect(() => {
    if (!token) {
      setPaymentProvidersLoading(false)
      setPaymentProvidersError('')
      setPaymentProviders([])
      setProvider('')
      return
    }

    setPaymentProvidersLoading(true)
    api.getPublicPaymentProviders()
      .then((items) => {
        setPaymentProviders(items)
        setPaymentProvidersError('')
        setProvider((current) => current && items.some((item) => item.provider === current) ? current : (items[0]?.provider ?? ''))
      })
      .catch((e: Error) => {
        setPaymentProviders([])
        setProvider('')
        setPaymentProvidersError(e.message)
      })
      .finally(() => setPaymentProvidersLoading(false))
  }, [token])


  const handleAuthSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setBusy(true)
    setError('')
    setNotice('')
    try {
      const response = authMode === 'login'
        ? await api.login(authEmail.trim(), authPassword)
        : await api.register(authEmail.trim(), authPassword, authDisplayName.trim() || authEmail.trim())
      await storeSession(response)
      setAuthPassword('')
      setNotice(authMode === 'login' ? 'Вход выполнен.' : 'Аккаунт создан.')
    } catch (e) {
      setError(e instanceof Error ? e.message : authMode === 'login' ? 'Не удалось войти' : 'Не удалось зарегистрироваться')
    } finally {
      setBusy(false)
    }
  }

  const switchAuthMode = (nextMode: 'login' | 'register') => {
    setAuthMode(nextMode)
    setError('')
    setNotice('')
  }

  const handleAuthTabsKeyDown = (event: React.KeyboardEvent<HTMLDivElement>) => {
    const modes: Array<'login' | 'register'> = ['login', 'register']
    const currentIndex = modes.indexOf(authMode)
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
    const nextTabId = nextMode === 'login' ? 'cabinet-auth-login-tab' : 'cabinet-auth-register-tab'
    window.requestAnimationFrame(() => document.getElementById(nextTabId)?.focus())
  }

  const handleRefreshSession = async () => {
    if (!refreshToken) {
      setError('Сессия не найдена. Войдите заново.')
      return
    }
    setBusy(true)
    setError('')
    setNotice('')
    try {
      const response = await api.refresh(refreshToken)
      setToken(response.accessToken)
      setRefreshToken(response.refreshToken)
      writeSessionStorageItem(TOKEN_STORAGE_KEY, response.accessToken)
      writeSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY, response.refreshToken)
      await loadAll(response.accessToken)
      setNotice('Сессия обновлена.')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось обновить сессию')
    } finally {
      setBusy(false)
    }
  }

  const handleLogout = async () => {
    setBusy(true)
    setError('')
    setNotice('')
    try {
      await api.logout(token || null, refreshToken || null)
      clearSession()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось выйти')
    } finally {
      setBusy(false)
    }
  }

  const handleForgotPassword = async () => {
    setBusy(true)
    setError('')
    setNotice('')
    try {
      const response = await api.forgotPassword(resetEmail)
      if (response.validationResetToken) setResetToken(response.validationResetToken)
      setNotice(response.message)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось запросить сброс пароля')
    } finally {
      setBusy(false)
    }
  }

  const handleResetPassword = async () => {
    setBusy(true)
    setError('')
    setNotice('')
    try {
      await api.resetPassword(resetToken, newPassword)
      setNewPassword('')
      setNotice('Пароль изменён. Войдите с новым паролем.')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось сбросить пароль')
    } finally {
      setBusy(false)
    }
  }

  const handleLoadQr = async (accessId: string) => {
    if (!token) return
    setBusy(true)
    setError('')
    setNotice('')
    try {
      const svg = await api.getMyAccessQrSvg(token, accessId)
      setQrSvgs((current) => ({ ...current, [accessId]: svg }))
      setNotice('QR-код загружен.')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось загрузить QR')
    } finally {
      setBusy(false)
    }
  }

  const handleCreateTelegramLink = async () => {
    if (!token) return
    setBusy(true)
    setError('')
    setNotice('')
    try {
      const link = await api.createTelegramLinkToken(token)
      setTelegramLink(link)
      await loadAll(token)
      setNotice('Ссылка на Telegram-бота создана.')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось создать ссылку Telegram')
    } finally {
      setBusy(false)
    }
  }

  const handleUnlinkTelegram = async () => {
    if (!token) return
    setBusy(true)
    setError('')
    setNotice('')
    try {
      const status = await api.unlinkTelegram(token)
      setTelegramStatus(status)
      setTelegramLink(null)
      setNotice('Telegram отвязан от аккаунта.')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось отвязать Telegram')
    } finally {
      setBusy(false)
    }
  }

  const handleRetryOrderPayment = async (order: OrderDto) => {
    if (!token) return
    if (!provider) {
      setError('Нет доступных платежных провайдеров для повторной оплаты.')
      return
    }
    setBusy(true)
    setError('')
    setNotice('')
    try {
      const payment = await api.initMyPayment(token, order.id, provider, window.location.href)
      window.open(payment.redirectUrl, '_blank', 'noopener,noreferrer')
      await loadAll(token)
      setNotice('Платеж открыт в новой вкладке.')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось повторить оплату')
    } finally {
      setBusy(false)
    }
  }

  const handleRenew = async (subscription: SubscriptionDto) => {
    if (!token) return
    if (!provider) {
      setError('Нет доступных платежных провайдеров для продления.')
      return
    }

    setBusy(true)
    setError('')
    setNotice('')

    try {
      const order = await api.createMyOrder(token, {
        tariffId: subscription.tariffId,
        type: 'Renewal',
        channel: 'Web',
        paymentProvider: provider,
        promoCode: null,
        isFirstPurchase: false
      })
      const payment = await api.initMyPayment(token, order.id, provider, window.location.origin)
      setRenewalState({ subscriptionId: subscription.id, order, payment })
      await loadAll(token)
      setNotice('Продление создано. Откройте оплату в карточке последнего продления.')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось создать продление')
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <SkipLink />
      <header className="topbar">
        <a className="app-brand" href={publicWebUrl}>VPN Platform</a>
        <nav aria-label="Основная навигация">
          <a href={`${publicWebUrl}/tariffs`}>Тарифы</a>
          <a href={`${publicWebUrl}/faq`}>Помощь</a>
          <a className="active" href="/" aria-current="page">Кабинет</a>
          {token && <button type="button" className="nav-button" onClick={() => setAppVersionOpenSignal((value) => value + 1)}>Что нового</button>}
        </nav>
      </header>

      <PageShell title="Личный кабинет">
      <div className="page-intro">
        <div>
          <h2 className="page-heading">Мой VPN-доступ</h2>
          <p className="muted no-margin-bottom">Подписки, ключи, QR-коды, платежи и Telegram-привязка в одном месте.</p>
        </div>
        <ValidationModeBadge label="Проверочный режим оплат" />
      </div>

      <div className="grid section">
        <StatTile label="Активных подписок" value={activeSubscriptions} />
        <StatTile label="Всего заказов" value={orders.length} />
        <StatTile label="Реферальных начислений" value={referrals.length} />
      </div>

      <div className="section">
        <Card>
          {!token ? (
            <>
              <div className="section-header">
                <div>
                  <h3>{authMode === 'login' ? 'Вход в личный кабинет' : 'Создать аккаунт'}</h3>
                  <p className="muted">Введите email и пароль. Сессия сохраняется только в этом браузере и очищается при выходе.</p>
                </div>
                <div className="segmented-control" role="tablist" aria-label="Режим авторизации" aria-orientation="horizontal" onKeyDown={handleAuthTabsKeyDown}>
                  <PrimaryButton id="cabinet-auth-login-tab" type="button" role="tab" className={authMode === 'login' ? 'active' : ''} aria-selected={authMode === 'login'} aria-controls={authPanelId} tabIndex={authMode === 'login' ? 0 : -1} onClick={() => switchAuthMode('login')}>Вход</PrimaryButton>
                  <PrimaryButton id="cabinet-auth-register-tab" type="button" role="tab" className={authMode === 'register' ? 'active' : ''} aria-selected={authMode === 'register'} aria-controls={authPanelId} tabIndex={authMode === 'register' ? 0 : -1} onClick={() => switchAuthMode('register')}>Регистрация</PrimaryButton>
                </div>
              </div>
              <form id={authPanelId} role="tabpanel" aria-labelledby={activeAuthTabId} aria-busy={busy} onSubmit={(event) => void handleAuthSubmit(event)}>
                {authMode === 'register' && (
                  <label>
                    <span>Имя</span>
                    <input value={authDisplayName} onChange={(e) => setAuthDisplayName(e.target.value)} placeholder="Как к вам обращаться" autoComplete="name" />
                  </label>
                )}
                <label>
                  <span>Email</span>
                  <input value={authEmail} onChange={(e) => setAuthEmail(e.target.value)} placeholder="you@example.com" type="email" autoComplete="email" required />
                  <small>На этот email будут привязаны покупки и продления.</small>
                </label>
                <PasswordField label="Пароль" value={authPassword} onChange={setAuthPassword} placeholder="Минимум 8 символов" autoComplete={authMode === 'login' ? 'current-password' : 'new-password'} minLength={8} required help="Сессия хранится только в этом браузере." />
                <div className="form-actions">
                  <PrimaryButton type="submit" disabled={busy || !authEmail || !authPassword} aria-busy={busy}>
                    {busy ? 'Проверяем...' : authMode === 'login' ? 'Войти' : 'Зарегистрироваться'}
                  </PrimaryButton>
                </div>
              </form>
            </>
          ) : (
            <>
              <h3>Сессия и оплата</h3>
              <div className="toolbar">
                <label>
                  <span>Способ оплаты для продления</span>
                  <select value={provider} disabled={paymentProvidersLoading || paymentProviders.length === 0} onChange={(e) => setProvider(e.target.value as PaymentProvider)}>
                    {paymentProviders.map((item) => (
                      <option key={item.provider} value={item.provider}>{item.publicName || item.provider}</option>
                    ))}
                  </select>
                </label>
                <PrimaryButton className="button-ghost" disabled={!refreshToken || busy} aria-busy={busy} onClick={() => void handleRefreshSession()}>Обновить сессию</PrimaryButton>
                <PrimaryButton disabled={!token || busy} aria-busy={busy} onClick={() => void loadAll(token)}>
                  {busy ? 'Обновляем...' : 'Обновить данные'}
                </PrimaryButton>
                <PrimaryButton disabled={busy} className="button-secondary" onClick={() => void handleLogout()}>Выйти</PrimaryButton>
              </div>
              <p className="muted">Вы вошли как {profile?.email ?? profile?.displayName ?? 'пользователь'}.</p>
              {paymentProvidersLoading && <p className="muted">Загружаем доступные способы оплаты...</p>}
              {paymentProvidersError && <p className="toast-error" role="alert">Не удалось загрузить способы оплаты: {paymentProvidersError}</p>}
              {!paymentProvidersLoading && paymentProviders.length === 0 && <p className="toast-error" role="alert">Нет включенных способов оплаты для оплат из кабинета.</p>}
            </>
          )}
          {busy && <LoadingBlock label="Загружаем кабинет..." />}
          {notice && <p className="toast-success" role="status" aria-live="polite">{notice}</p>}
          {error && <ErrorBlock message={error} />}
        </Card>
      </div>

      <div className="section">
        <Card>
          <h3>Сброс пароля</h3>
          <p className="muted">В проверочном режиме письмо не отправляется наружу: одноразовый код показывается сразу, чтобы можно было проверить сценарий локально.</p>
          <form className="form-grid" aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleResetPassword() }}>
            <label><span>Email</span><input value={resetEmail} onChange={(e) => setResetEmail(e.target.value)} placeholder="you@example.com" type="email" autoComplete="email" required /></label>
            <PasswordField label="Код сброса" value={resetToken} onChange={setResetToken} placeholder="Одноразовый код" autoComplete="one-time-code" />
            <PasswordField label="Новый пароль" value={newPassword} onChange={setNewPassword} placeholder="Новый пароль" autoComplete="new-password" minLength={8} />
            <div className="form-actions">
              <PrimaryButton type="button" className="button-ghost" disabled={!resetEmail || busy} aria-busy={busy} onClick={() => void handleForgotPassword()}>Запросить код</PrimaryButton>
              <PrimaryButton type="submit" disabled={!resetToken || !newPassword || busy} aria-busy={busy}>Сохранить пароль</PrimaryButton>
            </div>
          </form>
        </Card>
      </div>

      {profile && (
        <div className="section card-list">
          <Card>
            <h3>Профиль</h3>
            <p><strong>{profile.displayName}</strong></p>
            <p>{profile.email ?? 'email не указан'}</p>
            <p>Реферальный код: <strong>{profile.referralCode}</strong></p>
            <StatusBadge value={profile.status} />
          </Card>

          <Card>
            <h3>Telegram</h3>
            <p>Статус: <StatusBadge value={telegramStatus?.isLinked ? 'Linked' : 'NotLinked'} /></p>
            {telegramStatus?.isLinked ? (
              <>
                <p>@{telegramStatus.username ?? telegramStatus.telegramUserId}</p>
                <PrimaryButton disabled={busy} className="button-secondary" aria-busy={busy} onClick={() => void handleUnlinkTelegram()}>Отвязать Telegram</PrimaryButton>
              </>
            ) : (
              <>
                <PrimaryButton disabled={busy} aria-busy={busy} onClick={() => void handleCreateTelegramLink()}>Создать ссылку на бота</PrimaryButton>
                {telegramLink && (
                  <>
                    <p>Ссылка действует до {new Date(telegramLink.expiresAt).toLocaleString()}</p>
                    <div className="copy-row">
                      <a href={telegramLink.deepLinkUrl} target="_blank" rel="noreferrer" className="button" aria-label="Открыть Telegram-бота в новой вкладке">Открыть бота</a>
                      <CopyButton value={telegramLink.deepLinkUrl} label="Скопировать ссылку" />
                    </div>
                    <CodeBlock>{telegramLink.deepLinkUrl}</CodeBlock>
                  </>
                )}
              </>
            )}
          </Card>

          {renewalState && (
            <Card>
              <h3>Последнее продление</h3>
              <p>ID подписки: {renewalState.subscriptionId}</p>
              <p>Статус заказа: <StatusBadge value={renewalState.order.status} /></p>
              <p>ID платежа: {renewalState.payment.paymentId}</p>
              <div className="copy-row">
                <a href={renewalState.payment.redirectUrl} target="_blank" rel="noreferrer" className="button" aria-label="Открыть оплату в новой вкладке">Открыть оплату</a>
                <CopyButton value={renewalState.payment.redirectUrl} label="Скопировать ссылку" />
              </div>
              <CodeBlock>{renewalState.payment.redirectUrl}</CodeBlock>
            </Card>
          )}
        </div>
      )}

      <div className="section">
        <h2>Мои подписки</h2>
        {subscriptions.length === 0 && <EmptyState title="Подписок пока нет" description="Купите VPN в Telegram или на странице покупки — активная подписка появится здесь." />}
        <div className="card-list">
          {subscriptions.map((subscription) => (
            <div className="card" key={subscription.id}>
              <div className="card-head">
                <div>
                  <h3>{subscription.tariffName || subscription.status}</h3>
                  <p>Действует до: {new Date(subscription.endAt).toLocaleString()}</p>
                  <p>Сервер: {subscription.nodeName ?? 'не назначен'}</p>
                </div>
                <StatusBadge value={subscription.status} />
              </div>
              {subscription.accessUri && (
                <>
                  <CodeBlock>{subscription.accessUri}</CodeBlock>
                  <div className="toolbar">
                    <CopyButton value={subscription.accessUri} label="Скопировать ссылку" />
                    {subscription.currentAccessId && <PrimaryButton onClick={() => void handleLoadQr(subscription.currentAccessId ?? '')}>Показать QR-код</PrimaryButton>}
                  </div>
                </>
              )}
              {subscription.qrCodePath && <CodeBlock>QR-содержимое: {subscription.qrCodePath}</CodeBlock>}
              {subscription.currentAccessId && qrSvgs[subscription.currentAccessId] && <div className="qr-preview" dangerouslySetInnerHTML={{ __html: qrSvgs[subscription.currentAccessId] }} />}
              {subscription.configPath && <p>Конфигурация: {subscription.configPath}</p>}
              <p className="muted">Инструкция: импортируйте ссылку или QR-код в совместимый VLESS/Xray клиент. Если доступ требует проверки, дождитесь подтверждения администратора.</p>
              <div className="toolbar">
                <PrimaryButton disabled={busy || !provider} aria-busy={busy} onClick={() => void handleRenew(subscription)}>Продлить</PrimaryButton>
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="section">
        <h2>VPN-ключи</h2>
        {accesses.length === 0 && <EmptyState title="VPN-ключей пока нет" description="После успешной оплаты здесь появятся ссылка, QR-код и срок действия." />}
        <div className="card-list">
          {accesses.map((access) => (
            <Card key={access.id}>
              <div className="card-head">
                <div>
                  <h3>{access.serverName || access.providerType}</h3>
                  <p className="muted">Действует до {access.expiryDate ? new Date(access.expiryDate).toLocaleString() : '—'} · revision {access.revision}</p>
                </div>
                <StatusBadge value={access.status} />
              </div>
              {access.accessUri ? <CodeBlock>{access.accessUri}</CodeBlock> : <EmptyState title="Ссылка ещё не выдана" description="Если оплата прошла, обратитесь в поддержку." />}
              <div className="toolbar mt-12">
                <CopyButton value={access.accessUri} label="Скопировать ссылку" />
                <PrimaryButton disabled={busy || !access.id} aria-busy={busy} onClick={() => void handleLoadQr(access.id)}>Показать QR-код</PrimaryButton>
              </div>
              {qrSvgs[access.id] && <div className="qr-preview" dangerouslySetInnerHTML={{ __html: qrSvgs[access.id] }} />}
              <p className="muted">Инструкция: импортируйте ссылку или QR-код в совместимый Xray/VLESS клиент. Никому не пересылайте ключ.</p>
            </Card>
          ))}
        </div>
      </div>

      <div className="section card-list-two">
        <Card>
          <h3>История заказов</h3>
          <div className="list-stack">
            {orders.length === 0 && <EmptyState title="Заказов нет" description="Ваши покупки и продления появятся здесь." />}
            {orders.map((order) => (
              <div key={order.id} className="list-item">
                <div>
                  <strong>{order.amount} {order.currency}</strong>
                  <div className="muted">Тариф: {order.tariffId}</div>
                </div>
                <div className="status-stack">
                  <StatusBadge value={order.status} />
                  {(order.status === 'PendingPayment' || order.status === 'Failed') && (
                    <PrimaryButton disabled={busy || !provider} aria-busy={busy} onClick={() => void handleRetryOrderPayment(order)}>Повторить оплату</PrimaryButton>
                  )}
                </div>
              </div>
            ))}
          </div>
        </Card>

        <Card>
          <h3>История платежей</h3>
          <div className="list-stack">
            {payments.length === 0 && <EmptyState title="Платежей нет" description="История попыток оплаты появится после покупки или продления." />}
            {payments.map((payment) => (
              <div key={payment.id} className="list-item">
                <div>
                  <strong>{payment.provider}</strong>
                  <div className="muted">{payment.providerPaymentId}</div>
                </div>
                <StatusBadge value={payment.status} />
              </div>
            ))}
          </div>
        </Card>
      </div>

      <div className="section card-list-two">
        <Card>
          <h3>Выданные доступы</h3>
          <div className="list-stack">
            {accesses.length === 0 && <EmptyState title="Доступы не выдавались" description="Когда подписка будет активирована, здесь появятся ключи и QR-коды." />}
            {accesses.map((access) => (
              <div key={access.id} className="list-item-vertical">
                <div className="card-head">
                  <div>
                    <strong>{access.providerType}</strong>
                    <div className="muted">Версия: {access.revision}</div>
                  </div>
                  <StatusBadge value={access.status} />
                </div>
                {access.accessUri ? <CodeBlock>{access.accessUri}</CodeBlock> : <EmptyState title="Ключ ещё не готов" description="Доступ появится после обработки оплаты или синхронизации." />}
                {access.qrCodePath && <CodeBlock>QR-содержимое: {access.qrCodePath}</CodeBlock>}
                <div className="toolbar">
                  <CopyButton value={access.accessUri} label="Скопировать ссылку" />
                  <PrimaryButton disabled={busy || !access.accessUri} aria-busy={busy} onClick={() => void handleLoadQr(access.id)}>Показать QR-код</PrimaryButton>
                </div>
                {qrSvgs[access.id] && <div className="qr-preview" dangerouslySetInnerHTML={{ __html: qrSvgs[access.id] }} />}
                <p className="muted">Подключение: импортируйте строку выше в VLESS/Xray клиент или используйте QR-код.</p>
              </div>
            ))}
          </div>
        </Card>

        <Card>
          <h3>Реферальные начисления</h3>
          <div className="list-stack">
            {referrals.length === 0 && <EmptyState title="Реферальных начислений нет" description="Когда появятся начисления, они будут показаны здесь." />}
            {referrals.map((reward) => (
              <div key={reward.id} className="list-item">
                <div>
                  <strong>{reward.type}</strong>
                  <div className="muted">{reward.value} {reward.currencyOrUnit}</div>
                </div>
                <StatusBadge value={reward.status} />
              </div>
            ))}
          </div>
        </Card>
      </div>
      </PageShell>
      <AppVersionGate
        api={api}
        token={token}
        userId={profile?.id}
        manualOpenSignal={appVersionOpenSignal}
        onManualOpenHandled={() => setAppVersionOpenSignal(0)}
      />
    </>
  )
}
