import React, { useEffect, useMemo, useRef, useState } from 'react'
import {
  AccessCredentialDto,
  ApiClient,
  ApiClientError,
  AuthResponse,
  OrderDto,
  PaymentAttemptDto,
  PaymentInitResult,
  PaymentProvider,
  PublicPaymentProviderDto,
  RewardLedgerDto,
  SubscriptionDto,
  SupportConversationDto,
  SupportMessageDto,
  TelegramLinkTokenDto,
  TelegramStatusDto,
  UserProfileDto,
  translateAuthError,
  translateAuthMessage,
  validateAuthInput,
  validatePasswordResetConfirm,
  validatePasswordResetRequest
} from '@vpn-platform/api-client'
import { Card, CodeBlock, CopyButton, EmptyState, ErrorBlock, LoadingBlock, PageShell, PasswordField, PrimaryButton, SegmentedTabs, SkipLink, StatTile, StatusBadge, ValidationModeBadge } from '@vpn-platform/ui'
import { AppVersionGate } from './AppVersion'
import { buildCabinetSummary, formatReferralRewardType, getAccessQrAvailability, getCabinetAccessTerminalReason, getSubscriptionRenewalAvailability } from './cabinet-dashboard'
import { cabinetSessionEndedMessage, isCabinetSessionRejected } from './cabinet-session'
import { buildOrderExportText, formatPaymentMoney, getLatestPaymentForOrder, getOrderPaymentAvailability, getOrderStatusMessage, getPaymentStatusMessage, groupPaymentsByOrderId } from './cabinet-payments'
import { countOpenSupportConversations, getSupportStatusMessage, selectCurrentSupportConversation, validateSupportReply, validateSupportRequest } from './cabinet-support'

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
  const [supportConversations, setSupportConversations] = useState<SupportConversationDto[]>([])
  const [supportMessages, setSupportMessages] = useState<SupportMessageDto[]>([])
  const [selectedSupportConversationId, setSelectedSupportConversationId] = useState('')
  const [supportSubject, setSupportSubject] = useState('')
  const [supportText, setSupportText] = useState('')
  const [supportReplyText, setSupportReplyText] = useState('')
  const [supportOrderId, setSupportOrderId] = useState('')
  const [supportSubscriptionId, setSupportSubscriptionId] = useState('')
  const [supportLoading, setSupportLoading] = useState(false)
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
  const [authReferralCode, setAuthReferralCode] = useState('')
  const [renewalState, setRenewalState] = useState<RenewalState>(null)
  const [qrSvgs, setQrSvgs] = useState<Record<string, string>>({})
  const [resetEmail, setResetEmail] = useState('')
  const [resetToken, setResetToken] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [appVersionOpenSignal, setAppVersionOpenSignal] = useState(0)
  const restoredSessionHydrationStarted = useRef(false)
  const authPanelId = 'cabinet-auth-panel'
  const activeAuthTabId = authMode === 'login' ? 'cabinet-auth-login-tab' : 'cabinet-auth-register-tab'
  const authValidationErrors = validateAuthInput(authMode, authEmail, authPassword, authDisplayName)
  const resetRequestErrors = validatePasswordResetRequest(resetEmail)
  const resetConfirmErrors = validatePasswordResetConfirm(resetToken, newPassword)
  const showAuthValidation = authValidationErrors.length > 0 && Boolean(authEmail || authPassword || authDisplayName)
  const showResetValidation = Boolean(resetEmail || resetToken || newPassword)
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
  const cabinetSummary = useMemo(
    () => buildCabinetSummary(subscriptions, accesses),
    [subscriptions, accesses]
  )
  const paymentsByOrderId = useMemo(
    () => groupPaymentsByOrderId(payments),
    [payments]
  )
  const selectedSupportConversation = useMemo(
    () => selectCurrentSupportConversation(supportConversations, selectedSupportConversationId),
    [supportConversations, selectedSupportConversationId]
  )
  const openSupportConversations = useMemo(
    () => countOpenSupportConversations(supportConversations),
    [supportConversations]
  )
  const linkedCurrentAccess = cabinetSummary.currentSubscription?.currentAccessId
    ? accesses.find((access) => access.id === cabinetSummary.currentSubscription?.currentAccessId)
    : null
  const currentAccessTerminalReason = getCabinetAccessTerminalReason(linkedCurrentAccess ?? cabinetSummary.currentAccess, cabinetSummary.currentSubscription?.status)
  const currentConnectionLink = currentAccessTerminalReason
    ? ''
    : cabinetSummary.currentAccess?.accessUri ?? cabinetSummary.currentSubscription?.accessUri ?? ''
  const currentAccessId = currentAccessTerminalReason
    ? ''
    : cabinetSummary.currentAccess?.id ?? cabinetSummary.currentSubscription?.currentAccessId ?? ''
  const currentQrAvailability = getAccessQrAvailability(linkedCurrentAccess ?? cabinetSummary.currentAccess)

  const clearSession = () => {
    setToken('')
    setRefreshToken('')
    setProfile(null)
    setSubscriptions([])
    setOrders([])
    setPayments([])
    setAccesses([])
    setReferrals([])
    setSupportConversations([])
    setSupportMessages([])
    setSelectedSupportConversationId('')
    setTelegramStatus(null)
    setTelegramLink(null)
    setRenewalState(null)
    setQrSvgs({})
    removeSessionStorageItem(TOKEN_STORAGE_KEY)
    removeSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY)
  }

  const handleAuthenticatedError = (error: unknown, fallback: string) => {
    if (isCabinetSessionRejected(error)) {
      clearSession()
      setError(cabinetSessionEndedMessage)
      return
    }

    setError(error instanceof Error ? error.message : fallback)
  }

  const storeSession = async (response: AuthResponse) => {
    setToken(response.accessToken)
    setRefreshToken(response.refreshToken)
    writeSessionStorageItem(TOKEN_STORAGE_KEY, response.accessToken)
    writeSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY, response.refreshToken)
    return await loadAll(response.accessToken)
  }

  const loadAll = async (currentToken: string) => {
    if (!currentToken) return false

    setBusy(true)
    setError('')

    try {
      const [nextProfile, nextSubscriptions, nextOrders, nextPayments, nextAccesses, nextReferrals, nextSupportConversations, nextTelegramStatus] = await Promise.all([
        api.getMe(currentToken),
        api.getMySubscriptions(currentToken),
        api.getMyOrders(currentToken),
        api.getMyPayments(currentToken),
        api.getMyAccesses(currentToken),
        api.getMyReferrals(currentToken),
        api.getMySupportConversations(currentToken),
        api.getTelegramStatus(currentToken)
      ])

      setProfile(nextProfile)
      setSubscriptions(nextSubscriptions)
      setOrders(nextOrders)
      setPayments(nextPayments)
      setAccesses(nextAccesses)
      setReferrals(nextReferrals)
      setSupportConversations(nextSupportConversations)
      if (!selectedSupportConversationId && nextSupportConversations.length > 0) {
        setSelectedSupportConversationId(nextSupportConversations[0].id)
      }
      setTelegramStatus(nextTelegramStatus)
      return true
    } catch (e) {
      handleAuthenticatedError(e, 'Не удалось загрузить кабинет')
      return false
    } finally {
      setBusy(false)
    }
  }

  const loadSupportMessages = async (conversationId: string) => {
    if (!token || !conversationId) {
      setSupportMessages([])
      return false
    }

    setSupportLoading(true)
    try {
      setSupportMessages(await api.getMySupportMessages(token, conversationId))
      return true
    } catch (e) {
      handleAuthenticatedError(e, 'Не удалось загрузить переписку поддержки')
      return false
    } finally {
      setSupportLoading(false)
    }
  }

  useEffect(() => {
    if (restoredSessionHydrationStarted.current) return
    restoredSessionHydrationStarted.current = true
    // New login and refresh sessions are loaded by their handlers; this hydrates only a restored session.
    void loadAll(token)
  }, [])

  useEffect(() => {
    if (!token || !selectedSupportConversation?.id) {
      setSupportMessages([])
      return
    }

    void loadSupportMessages(selectedSupportConversation.id)
  }, [token, selectedSupportConversation?.id])

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
    if (authValidationErrors.length > 0) {
      setError(authValidationErrors.join(' '))
      return
    }

    setBusy(true)
    setError('')
    setNotice('')
    try {
      const response = authMode === 'login'
        ? await api.login(authEmail.trim(), authPassword)
        : await api.register(authEmail.trim(), authPassword, authDisplayName.trim() || authEmail.trim(), authReferralCode)
      if (!await storeSession(response)) return
      setAuthPassword('')
      setAuthReferralCode('')
      setNotice(authMode === 'login' ? 'Вход выполнен.' : 'Аккаунт создан.')
    } catch (e) {
      setError(translateAuthError(e, authMode === 'login' ? 'Не удалось войти' : 'Не удалось зарегистрироваться'))
    } finally {
      setBusy(false)
    }
  }

  const switchAuthMode = (nextMode: 'login' | 'register') => {
    setAuthMode(nextMode)
    setError('')
    setNotice('')
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
      if (!await loadAll(response.accessToken)) return
      setNotice('Сессия обновлена.')
    } catch (e) {
      handleAuthenticatedError(e, 'Не удалось обновить сессию')
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
    } catch {
      setError('Локальная сессия завершена, но отзыв серверной сессии не подтверждён. На чужом устройстве измените пароль из доверенного браузера.')
    } finally {
      clearSession()
      setBusy(false)
    }
  }

  const handleForgotPassword = async () => {
    if (resetRequestErrors.length > 0) {
      setError(resetRequestErrors.join(' '))
      return
    }

    setBusy(true)
    setError('')
    setNotice('')
    try {
      const response = await api.forgotPassword(resetEmail)
      if (response.validationResetToken) setResetToken(response.validationResetToken)
      setNotice(translateAuthMessage(response.message))
    } catch (e) {
      setError(translateAuthError(e, 'Не удалось запросить сброс пароля'))
    } finally {
      setBusy(false)
    }
  }

  const handleResetPassword = async () => {
    if (resetConfirmErrors.length > 0) {
      setError(resetConfirmErrors.join(' '))
      return
    }

    setBusy(true)
    setError('')
    setNotice('')
    try {
      await api.resetPassword(resetToken, newPassword)
      clearSession()
      setNewPassword('')
      setNotice('Пароль изменён. Войдите с новым паролем.')
    } catch (e) {
      setError(translateAuthError(e, 'Не удалось сбросить пароль'))
    } finally {
      setBusy(false)
    }
  }

  const handleLoadQr = async (accessId: string) => {
    if (!token) return
    const qrAvailability = getAccessQrAvailability(accesses.find((access) => access.id === accessId))
    if (!qrAvailability.canGenerate) {
      setError(qrAvailability.reason ?? 'QR-код пока недоступен.')
      return
    }
    setBusy(true)
    setError('')
    setNotice('')
    try {
      const svg = await api.getMyAccessQrSvg(token, accessId)
      setQrSvgs((current) => ({ ...current, [accessId]: svg }))
      setNotice('QR-код загружен.')
    } catch (e) {
      handleAuthenticatedError(e, 'Не удалось загрузить QR')
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
      if (!await loadAll(token)) return
      setNotice('Ссылка на Telegram-бота создана.')
    } catch (e) {
      handleAuthenticatedError(e, 'Не удалось создать ссылку Telegram')
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
      handleAuthenticatedError(e, 'Не удалось отвязать Telegram')
    } finally {
      setBusy(false)
    }
  }

  const handleCreateSupportConversation = async () => {
    if (!token) return
    const validationErrors = validateSupportRequest(supportSubject, supportText)
    if (validationErrors.length > 0) {
      setError(validationErrors.join(' '))
      return
    }

    setBusy(true)
    setError('')
    setNotice('')
    try {
      const conversation = await api.createMySupportConversation(token, {
        subject: supportSubject.trim(),
        text: supportText.trim(),
        orderId: supportOrderId || null,
        subscriptionId: supportSubscriptionId || null
      })
      setSupportConversations((current) => [conversation, ...current.filter((item) => item.id !== conversation.id)])
      setSelectedSupportConversationId(conversation.id)
      setSupportSubject('')
      setSupportText('')
      setSupportOrderId('')
      setSupportSubscriptionId('')
      if (!await loadSupportMessages(conversation.id)) return
      setNotice('Обращение в поддержку создано.')
    } catch (e) {
      handleAuthenticatedError(e, 'Не удалось создать обращение в поддержку')
    } finally {
      setBusy(false)
    }
  }

  const handleReplySupportConversation = async () => {
    if (!token || !selectedSupportConversation) return
    const validationErrors = validateSupportReply(supportReplyText)
    if (validationErrors.length > 0) {
      setError(validationErrors.join(' '))
      return
    }

    setBusy(true)
    setError('')
    setNotice('')
    try {
      const message = await api.replyMySupportConversation(token, selectedSupportConversation.id, supportReplyText.trim(), selectedSupportConversation.revision)
      setSupportMessages((current) => [...current, message])
      setSupportConversations((current) => current.map((item) => item.id === selectedSupportConversation.id ? { ...item, status: 'open', revision: item.revision + 1, updatedAt: new Date().toISOString(), closedAt: null } : item))
      setSupportReplyText('')
      setNotice('Сообщение отправлено в поддержку.')
    } catch (e) {
      if (e instanceof ApiClientError && e.status === 409) await loadAll(token)
      handleAuthenticatedError(e, 'Не удалось отправить сообщение в поддержку')
    } finally {
      setBusy(false)
    }
  }

  const handleSupportConversationStatus = async (status: 'open' | 'closed') => {
    if (!token || !selectedSupportConversation) return
    setBusy(true)
    setError('')
    setNotice('')
    try {
      const result = await api.updateMySupportConversationStatus(token, selectedSupportConversation.id, status, selectedSupportConversation.revision)
      setSupportConversations((current) => current.map((item) => item.id === selectedSupportConversation.id ? { ...item, status: result.status, revision: result.revision, closedAt: result.status === 'closed' ? new Date().toISOString() : null, updatedAt: new Date().toISOString() } : item))
      setNotice(result.status === 'closed' ? 'Обращение закрыто.' : 'Обращение переоткрыто.')
    } catch (e) {
      if (e instanceof ApiClientError && e.status === 409) await loadAll(token)
      handleAuthenticatedError(e, 'Не удалось изменить статус обращения')
    } finally {
      setBusy(false)
    }
  }

  const handleRetryOrderPayment = async (order: OrderDto) => {
    if (!token) return
    const paymentAvailability = getOrderPaymentAvailability(order)
    if (!paymentAvailability.canRetry) {
      setError(paymentAvailability.reason ?? 'Этот заказ нельзя оплатить повторно. Создайте новый заказ.')
      return
    }
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
      if (!await loadAll(token)) return
      setNotice('Платеж открыт в новой вкладке.')
    } catch (e) {
      handleAuthenticatedError(e, 'Не удалось повторить оплату')
    } finally {
      setBusy(false)
    }
  }

  const handleRenew = async (subscription: SubscriptionDto) => {
    if (!token) return
    const renewalAvailability = getSubscriptionRenewalAvailability(subscription)
    if (!renewalAvailability.canRenew) {
      setError(renewalAvailability.reason ?? 'Эту подписку нельзя продлить.')
      return
    }
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
        paymentProvider: provider,
        promoCode: null,
        subscriptionId: subscription.id
      })
      const payment = await api.initMyPayment(token, order.id, provider, window.location.origin)
      setRenewalState({ subscriptionId: subscription.id, order, payment })
      if (!await loadAll(token)) return
      setNotice('Продление создано. Откройте оплату в карточке последнего продления.')
    } catch (e) {
      handleAuthenticatedError(e, 'Не удалось создать продление')
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
        <StatTile label="Открытых обращений" value={openSupportConversations} />
      </div>

      <div id="user-help" className="section">
        <Card className="cabinet-help-card">
          <div>
            <p className="eyebrow">Помощь</p>
            <h3>Как пользоваться сервисом</h3>
            <p className="muted">Основной путь: выбрать тариф, оплатить заказ, вернуться в кабинет и забрать ссылку подключения.</p>
          </div>
          <ol className="cabinet-help-steps">
            <li><strong>Оплата.</strong> Выберите доступный способ оплаты и откройте ссылку платежа.</li>
            <li><strong>VPN-доступ.</strong> Скопируйте ссылку или откройте QR-код в совместимом VPN-клиенте.</li>
            <li><strong>Продление.</strong> Нажмите "Продлить" у активной подписки и оплатите новый заказ.</li>
            <li><strong>Поддержка.</strong> Создайте обращение в поддержку, если платеж завис или ссылка не появилась.</li>
          </ol>
          <a className="button button-ghost" href={`${publicWebUrl}/help`}>Открыть полную инструкцию</a>
        </Card>
      </div>

      {token && (
        <div className="section">
          <Card className="cabinet-current-card">
            <div className="cabinet-current-main">
              <p className="eyebrow">Текущий VPN-доступ</p>
              {cabinetSummary.currentSubscription ? (
                <>
                  <div className="card-head">
                    <div>
                      <h3>{cabinetSummary.currentSubscription.tariffName || 'Активная подписка'}</h3>
                      <p className="muted">
                        Действует до {new Date(cabinetSummary.currentSubscription.endAt).toLocaleString()}
                        {cabinetSummary.daysLeft !== null ? ` · осталось ${cabinetSummary.daysLeft} дн.` : ''}
                      </p>
                      <p className="muted">Сервер: {cabinetSummary.currentSubscription.nodeName ?? cabinetSummary.currentAccess?.serverName ?? 'ожидает назначения'}</p>
                    </div>
                    <StatusBadge value={cabinetSummary.currentSubscription.status} />
                  </div>
                  {currentConnectionLink ? (
                    <>
                      <CodeBlock>{currentConnectionLink}</CodeBlock>
                      <div className="toolbar mt-12">
                        <CopyButton value={currentConnectionLink} label="Скопировать ссылку" />
                        {currentAccessId && <PrimaryButton disabled={busy || !currentQrAvailability.canGenerate} title={currentQrAvailability.reason ?? undefined} aria-busy={busy} onClick={() => void handleLoadQr(currentAccessId)}>Показать QR-код</PrimaryButton>}
                        <PrimaryButton disabled={busy || !provider} aria-busy={busy} className="button-secondary" onClick={() => void handleRenew(cabinetSummary.currentSubscription!)}>Продлить</PrimaryButton>
                      </div>
                      {currentAccessId && qrSvgs[currentAccessId] && <div className="qr-preview" dangerouslySetInnerHTML={{ __html: qrSvgs[currentAccessId] }} />}
                    </>
                  ) : currentAccessTerminalReason ? (
                    <p className="safe-note" role="status">{currentAccessTerminalReason}</p>
                  ) : (
                    <EmptyState title="Ключ ещё готовится" description="После подтверждения оплаты ссылка подключения и QR-код появятся здесь автоматически." />
                  )}
                </>
              ) : (
                <EmptyState
                  title="Подписки пока нет"
                  description="Выберите тариф, оплатите заказ и вернитесь в кабинет: здесь появятся статус подписки, ссылка и QR-код."
                  action={<a className="button" href={`${publicWebUrl}/tariffs`}>Выбрать тариф</a>}
                />
              )}
            </div>
            <div className="cabinet-current-guide">
              <strong>Как подключиться</strong>
              <ol>
                <li>Скопируйте ссылку или откройте QR-код.</li>
                <li>Импортируйте ключ в VLESS/Xray-совместимый клиент.</li>
                <li>Не пересылайте ключ другим людям.</li>
              </ol>
            </div>
          </Card>
        </div>
      )}

      <div className="section">
        <Card>
          {!token ? (
            <>
              <div className="section-header">
                <div>
                  <h3>{authMode === 'login' ? 'Вход в личный кабинет' : 'Создать аккаунт'}</h3>
                  <p className="muted">Введите email и пароль. Сессия сохраняется только в этом браузере и очищается при выходе.</p>
                </div>
                <SegmentedTabs
                  idPrefix="cabinet-auth"
                  panelId={authPanelId}
                  label="Режим авторизации"
                  value={authMode}
                  onChange={switchAuthMode}
                  options={[
                    { value: 'login', label: 'Вход' },
                    { value: 'register', label: 'Регистрация' }
                  ]}
                />
              </div>
              <form id={authPanelId} role="tabpanel" aria-labelledby={activeAuthTabId} aria-busy={busy} onSubmit={(event) => void handleAuthSubmit(event)}>
                {authMode === 'register' && (
                  <label>
                    <span>Имя</span>
                    <input value={authDisplayName} onChange={(e) => setAuthDisplayName(e.target.value)} placeholder="Как к вам обращаться" autoComplete="name" />
                  </label>
                )}
                {authMode === 'register' && (
                  <label>
                    <span>Реферальный код</span>
                    <input value={authReferralCode} onChange={(e) => setAuthReferralCode(e.target.value)} placeholder="Необязательно" autoComplete="off" />
                    <small>Укажите код пользователя, который вас пригласил.</small>
                  </label>
                )}
                <label>
                  <span>Email</span>
                  <input value={authEmail} onChange={(e) => setAuthEmail(e.target.value)} placeholder="you@example.com" type="email" autoComplete="email" required />
                  <small>На этот email будут привязаны покупки и продления.</small>
                </label>
                <PasswordField label="Пароль" value={authPassword} onChange={setAuthPassword} placeholder="Минимум 8 символов" autoComplete={authMode === 'login' ? 'current-password' : 'new-password'} minLength={8} required help="Сессия хранится только в этом браузере." />
                {showAuthValidation && (
                  <ul className="validation-list" aria-live="polite">
                    {authValidationErrors.map((item) => <li key={item}>{item}</li>)}
                  </ul>
                )}
                <div className="form-actions">
                  <PrimaryButton type="submit" disabled={busy || authValidationErrors.length > 0} aria-busy={busy}>
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
                      <option key={item.provider} value={item.provider}>{item.publicName || item.provider}{item.mode === 'Sandbox' ? ' · проверка' : ''}</option>
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
              {!paymentProvidersLoading && paymentProviders.length > 0 && <p className="muted">Доступно способов оплаты: {paymentProviders.length}. В списке только включенные и готовые web-провайдеры.</p>}
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
          <p className="muted">Код восстановления придёт на email. В локальном режиме проверки он может быть показан сразу.</p>
          <form className="form-grid" aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleResetPassword() }}>
            <label><span>Email</span><input value={resetEmail} onChange={(e) => setResetEmail(e.target.value)} placeholder="you@example.com" type="email" autoComplete="email" required /></label>
            <PasswordField label="Код сброса" value={resetToken} onChange={setResetToken} placeholder="Одноразовый код" autoComplete="one-time-code" />
            <PasswordField label="Новый пароль" value={newPassword} onChange={setNewPassword} placeholder="Новый пароль" autoComplete="new-password" minLength={8} />
            {showResetValidation && [...resetRequestErrors, ...resetConfirmErrors].length > 0 && (
              <ul className="validation-list" aria-live="polite">
                {[...resetRequestErrors, ...resetConfirmErrors].map((item) => <li key={item}>{item}</li>)}
              </ul>
            )}
            <div className="form-actions">
              <PrimaryButton type="button" className="button-ghost" disabled={resetRequestErrors.length > 0 || busy} aria-busy={busy} onClick={() => void handleForgotPassword()}>Запросить код</PrimaryButton>
              <PrimaryButton type="submit" disabled={resetConfirmErrors.length > 0 || busy} aria-busy={busy}>Сохранить пароль</PrimaryButton>
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
        {subscriptions.length === 0 && (
          <EmptyState
            title="Подписок пока нет"
            description="Купите VPN на странице тарифов — активная подписка появится здесь."
            action={<a className="button" href={`${publicWebUrl}/tariffs`}>Перейти к тарифам</a>}
          />
        )}
        <div className="card-list">
          {subscriptions.map((subscription) => {
            const renewalAvailability = getSubscriptionRenewalAvailability(subscription)
            const currentAccess = accesses.find((access) => access.id === subscription.currentAccessId)
            const qrAvailability = getAccessQrAvailability(currentAccess)
            const terminalReason = getCabinetAccessTerminalReason(currentAccess, subscription.status)
            const isTerminal = Boolean(terminalReason)
            return <div className="card" key={subscription.id}>
              <div className="card-head">
                <div>
                  <h3>{subscription.tariffName || subscription.status}</h3>
                  <p>Действует до: {new Date(subscription.endAt).toLocaleString()}</p>
                  <p>Сервер: {subscription.nodeName ?? 'не назначен'}</p>
                </div>
                <StatusBadge value={subscription.status} />
              </div>
              {subscription.accessUri && !isTerminal && (
                <>
                  <CodeBlock>{subscription.accessUri}</CodeBlock>
                  <div className="toolbar">
                    <CopyButton value={subscription.accessUri} label="Скопировать ссылку" />
                    {subscription.currentAccessId && <PrimaryButton disabled={busy || !qrAvailability.canGenerate} title={qrAvailability.reason ?? undefined} aria-busy={busy} onClick={() => void handleLoadQr(subscription.currentAccessId ?? '')}>Показать QR-код</PrimaryButton>}
                  </div>
                </>
              )}
              {isTerminal && <p className="safe-note" role="status">{terminalReason}</p>}
              {!isTerminal && subscription.qrCodePath && <CodeBlock>QR-содержимое: {subscription.qrCodePath}</CodeBlock>}
              {!isTerminal && subscription.currentAccessId && qrSvgs[subscription.currentAccessId] && <div className="qr-preview" dangerouslySetInnerHTML={{ __html: qrSvgs[subscription.currentAccessId] }} />}
              {!isTerminal && subscription.configPath && <p>Конфигурация: {subscription.configPath}</p>}
              {!isTerminal && <p className="muted">Инструкция: импортируйте ссылку или QR-код в совместимый VLESS/Xray клиент. Если доступ требует проверки, дождитесь подтверждения администратора.</p>}
              {renewalAvailability.canRenew ? (
                <div className="toolbar">
                  <PrimaryButton disabled={busy || !provider} aria-busy={busy} onClick={() => void handleRenew(subscription)}>Продлить</PrimaryButton>
                </div>
              ) : (
                <p className="safe-note" role="status">{renewalAvailability.reason}</p>
              )}
            </div>
          })}
        </div>
      </div>

      <div className="section">
        <h2>VPN-ключи</h2>
        {accesses.length === 0 && <EmptyState title="VPN-ключей пока нет" description="После успешной оплаты здесь появятся ссылка, QR-код и срок действия." />}
        <div className="card-list">
          {accesses.map((access) => {
            const qrAvailability = getAccessQrAvailability(access)
            const terminalReason = getCabinetAccessTerminalReason(access)
            const isTerminal = Boolean(terminalReason)
            return <Card key={access.id}>
              <div className="card-head">
                <div>
                  <h3>{access.serverName || access.providerType}</h3>
                  <p className="muted">Действует до {access.expiryDate ? new Date(access.expiryDate).toLocaleString() : '—'} · revision {access.revision}</p>
                </div>
                <StatusBadge value={access.status} />
              </div>
              {isTerminal
                ? <p className="safe-note" role="status">{terminalReason}</p>
                : access.accessUri ? <CodeBlock>{access.accessUri}</CodeBlock> : <EmptyState title="Ссылка ещё не выдана" description="Если оплата прошла, обратитесь в поддержку." />}
              {!isTerminal && <div className="toolbar mt-12">
                <CopyButton value={access.accessUri} label="Скопировать ссылку" />
                <PrimaryButton disabled={busy || !qrAvailability.canGenerate} title={qrAvailability.reason ?? undefined} aria-busy={busy} onClick={() => void handleLoadQr(access.id)}>Показать QR-код</PrimaryButton>
              </div>}
              {!isTerminal && qrSvgs[access.id] && <div className="qr-preview" dangerouslySetInnerHTML={{ __html: qrSvgs[access.id] }} />}
              {!isTerminal && <p className="muted">Инструкция: импортируйте ссылку или QR-код в совместимый Xray/VLESS клиент. Никому не пересылайте ключ.</p>}
            </Card>
          })}
        </div>
      </div>

      <div className="section card-list-two">
        <Card>
          <h3>История заказов</h3>
          <div className="list-stack">
            {orders.length === 0 && <EmptyState title="Заказов нет" description="Ваши покупки и продления появятся здесь." />}
            {orders.map((order) => {
              const orderPayments = paymentsByOrderId.get(order.id) ?? []
              const latestPayment = getLatestPaymentForOrder(order, orderPayments)
              const paymentAvailability = getOrderPaymentAvailability(order)
              const exportText = buildOrderExportText(order, orderPayments)
              const exportHref = `data:application/json;charset=utf-8,${encodeURIComponent(exportText)}`

              return (
                <div key={order.id} className="list-item-vertical payment-record">
                  <div className="card-head">
                    <div>
                      <strong>{formatPaymentMoney(order.amount, order.currency)}</strong>
                      <div className="muted">Тариф: {order.tariffName || order.tariffId}</div>
                    </div>
                    <StatusBadge value={paymentAvailability.isExpired ? 'Expired' : order.status} />
                  </div>
                  <p className="muted no-margin-bottom">{paymentAvailability.reason ?? getOrderStatusMessage(order.status)}</p>
                  <dl className="payment-meta-grid">
                    <div><dt>Тип</dt><dd>{order.type ?? '—'}</dd></div>
                    <div><dt>Канал</dt><dd>{order.channel ?? '—'}</dd></div>
                    <div><dt>Провайдер</dt><dd>{(order.paymentProvider ?? provider) || '—'}</dd></div>
                    <div><dt>Истекает</dt><dd>{new Date(order.expiresAt).toLocaleString()}</dd></div>
                    <div><dt>Оплачен</dt><dd>{order.paidAt ? new Date(order.paidAt).toLocaleString() : '—'}</dd></div>
                    <div><dt>Попыток оплаты</dt><dd>{order.paymentAttemptsCount ?? orderPayments.length}</dd></div>
                  </dl>
                  {latestPayment && (
                    <div className="payment-related">
                      <span>Последний платеж</span>
                      <strong>{latestPayment.provider} · {formatPaymentMoney(latestPayment.amount, latestPayment.currency)}</strong>
                      <p className="muted no-margin-bottom">{getPaymentStatusMessage(latestPayment.status)}</p>
                    </div>
                  )}
                  <div className="toolbar compact">
                    {paymentAvailability.canRetry && (
                      <PrimaryButton disabled={busy || !provider} aria-busy={busy} onClick={() => void handleRetryOrderPayment(order)}>Повторить оплату</PrimaryButton>
                    )}
                    {paymentAvailability.shouldCreateNewOrder && <a className="button" href={`${publicWebUrl}/tariffs`}>Создать новый заказ</a>}
                    <CopyButton value={exportText} label="Скопировать данные" />
                    <a className="button button-ghost" download={`order-${order.id}.json`} href={exportHref}>Скачать JSON</a>
                  </div>
                </div>
              )
            })}
          </div>
        </Card>

        <Card>
          <h3>История платежей</h3>
          <div className="list-stack">
            {payments.length === 0 && <EmptyState title="Платежей нет" description="История попыток оплаты появится после покупки или продления." />}
            {payments.map((payment) => (
              <div key={payment.id} className="list-item-vertical payment-record">
                <div className="card-head">
                  <div>
                    <strong>{payment.provider} · {formatPaymentMoney(payment.amount, payment.currency)}</strong>
                    <div className="muted">Заказ: {payment.orderId}</div>
                  </div>
                  <StatusBadge value={payment.status} />
                </div>
                <p className="muted no-margin-bottom">{getPaymentStatusMessage(payment.status)}</p>
                <dl className="payment-meta-grid">
                  <div><dt>ID у провайдера</dt><dd>{payment.providerPaymentId || '—'}</dd></div>
                  <div><dt>Режим</dt><dd>{payment.providerMode ?? '—'}</dd></div>
                  <div><dt>Создан</dt><dd>{new Date(payment.createdAt).toLocaleString()}</dd></div>
                  <div><dt>Оплачен</dt><dd>{payment.paidAt ? new Date(payment.paidAt).toLocaleString() : '—'}</dd></div>
                  <div><dt>Ошибка</dt><dd>{payment.failedAt ? new Date(payment.failedAt).toLocaleString() : payment.statusReason ?? '—'}</dd></div>
                  <div><dt>Активация</dt><dd>{payment.isActivationProcessed ? 'обработана' : 'ожидает'}</dd></div>
                </dl>
                {payment.confirmationUrl && (
                  <div className="toolbar compact">
                    <a className="button" href={payment.confirmationUrl} target="_blank" rel="noreferrer">Открыть оплату</a>
                    <CopyButton value={payment.confirmationUrl} label="Скопировать ссылку" />
                  </div>
                )}
              </div>
            ))}
          </div>
        </Card>
      </div>

      {profile && (
        <div className="section card-list-two">
          <Card>
            <div className="section-header">
              <div>
                <h3>Поддержка</h3>
                <p className="muted">Создайте обращение из кабинета. Telegram не нужен: ответ появится в этой переписке.</p>
              </div>
              <StatusBadge value={`${openSupportConversations} open`} />
            </div>
            <form className="form-grid" aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleCreateSupportConversation() }}>
              <label>
                <span>Тема</span>
                <input value={supportSubject} onChange={(e) => setSupportSubject(e.target.value)} placeholder="Например, не прошла оплата" maxLength={160} required />
              </label>
              <label>
                <span>Связанный заказ</span>
                <select value={supportOrderId} onChange={(e) => setSupportOrderId(e.target.value)}>
                  <option value="">Без привязки к заказу</option>
                  {orders.map((order) => (
                    <option key={order.id} value={order.id}>{order.tariffName || order.tariffId} · {formatPaymentMoney(order.amount, order.currency)} · {order.status}</option>
                  ))}
                </select>
              </label>
              <label>
                <span>Связанная подписка</span>
                <select value={supportSubscriptionId} onChange={(e) => setSupportSubscriptionId(e.target.value)}>
                  <option value="">Без привязки к подписке</option>
                  {subscriptions.map((subscription) => (
                    <option key={subscription.id} value={subscription.id}>{subscription.tariffName || subscription.tariffId} · {subscription.status}</option>
                  ))}
                </select>
              </label>
              <label className="full-width">
                <span>Сообщение</span>
                <textarea value={supportText} onChange={(e) => setSupportText(e.target.value)} rows={4} placeholder="Опишите проблему: что делали, какой тариф или платеж проверяли, что увидели на экране." maxLength={4000} required />
              </label>
              <div className="form-actions">
                <PrimaryButton type="submit" disabled={busy || validateSupportRequest(supportSubject, supportText).length > 0} aria-busy={busy}>Создать обращение</PrimaryButton>
              </div>
            </form>
          </Card>

          <Card>
            <div className="section-header">
              <div>
                <h3>Мои обращения</h3>
                {selectedSupportConversation && <p className="muted no-margin-bottom">{getSupportStatusMessage(selectedSupportConversation.status)}</p>}
              </div>
              {selectedSupportConversation && <StatusBadge value={selectedSupportConversation.status} />}
            </div>
            <div className="support-layout">
              <div className="list-stack">
                {supportConversations.length === 0 && <EmptyState title="Обращений нет" description="Когда вы напишете в поддержку, переписка появится здесь." />}
                {supportConversations.map((conversation) => (
                  <button
                    key={conversation.id}
                    type="button"
                    className={`support-ticket${selectedSupportConversation?.id === conversation.id ? ' selected-item' : ''}`}
                    aria-pressed={selectedSupportConversation?.id === conversation.id}
                    aria-label={`${conversation.subject || 'Обращение в поддержку'}, статус ${conversation.status}`}
                    onClick={() => setSelectedSupportConversationId(conversation.id)}
                  >
                    <span>
                      <strong>{conversation.subject || 'Обращение в поддержку'}</strong>
                      <small>{conversation.channel} · {new Date(conversation.updatedAt).toLocaleString()}</small>
                    </span>
                    <StatusBadge value={conversation.status} />
                  </button>
                ))}
              </div>

              <div className="support-thread">
                {supportLoading && <LoadingBlock label="Загружаем переписку..." />}
                {!supportLoading && selectedSupportConversation && supportMessages.length === 0 && <EmptyState title="Сообщений нет" description="Переписка появится после первого сообщения." />}
                {!selectedSupportConversation && supportConversations.length > 0 && <EmptyState title="Выберите обращение" description="Откройте обращение из списка, чтобы увидеть переписку." />}
                {supportMessages.map((message) => (
                  <div key={message.id} className={`support-message support-message-${message.direction}`}>
                    <div className="card-head">
                      <strong>{message.direction === 'outbound' ? 'Поддержка' : 'Вы'}</strong>
                      <span className="muted">{new Date(message.createdAt).toLocaleString()}</span>
                    </div>
                    <p>{message.text}</p>
                  </div>
                ))}
                {selectedSupportConversation && (
                  <form className="mt-12" aria-busy={busy} onSubmit={(event) => { event.preventDefault(); void handleReplySupportConversation() }}>
                    <label>
                      <span>Ответ</span>
                      <textarea value={supportReplyText} onChange={(e) => setSupportReplyText(e.target.value)} rows={3} placeholder="Напишите уточнение или ответ поддержке" maxLength={4000} />
                    </label>
                    <div className="toolbar compact mt-12">
                      <PrimaryButton type="submit" disabled={busy || validateSupportReply(supportReplyText).length > 0} aria-busy={busy}>Отправить</PrimaryButton>
                      {selectedSupportConversation.status === 'closed'
                        ? <PrimaryButton type="button" className="button-secondary" disabled={busy} aria-busy={busy} onClick={() => void handleSupportConversationStatus('open')}>Переоткрыть</PrimaryButton>
                        : <PrimaryButton type="button" className="button-ghost" disabled={busy} aria-busy={busy} onClick={() => void handleSupportConversationStatus('closed')}>Закрыть</PrimaryButton>}
                    </div>
                  </form>
                )}
              </div>
            </div>
          </Card>
        </div>
      )}

      <div className="section card-list-two">
        <Card>
          <h3>Выданные доступы</h3>
          <div className="list-stack">
            {accesses.length === 0 && <EmptyState title="Доступы не выдавались" description="Когда подписка будет активирована, здесь появятся ключи и QR-коды." />}
            {accesses.map((access) => {
              const qrAvailability = getAccessQrAvailability(access)
              const terminalReason = getCabinetAccessTerminalReason(access)
              const isTerminal = Boolean(terminalReason)
              return <div key={access.id} className="list-item-vertical">
                <div className="card-head">
                  <div>
                    <strong>{access.providerType}</strong>
                    <div className="muted">Версия: {access.revision}</div>
                  </div>
                  <StatusBadge value={access.status} />
                </div>
                {isTerminal
                  ? <p className="safe-note" role="status">{terminalReason}</p>
                  : access.accessUri ? <CodeBlock>{access.accessUri}</CodeBlock> : <EmptyState title="Ключ ещё не готов" description="Доступ появится после обработки оплаты или синхронизации." />}
                {!isTerminal && access.qrCodePath && <CodeBlock>QR-содержимое: {access.qrCodePath}</CodeBlock>}
                {!isTerminal && <div className="toolbar">
                  <CopyButton value={access.accessUri} label="Скопировать ссылку" />
                  <PrimaryButton disabled={busy || !qrAvailability.canGenerate} title={qrAvailability.reason ?? undefined} aria-busy={busy} onClick={() => void handleLoadQr(access.id)}>Показать QR-код</PrimaryButton>
                </div>}
                {!isTerminal && qrSvgs[access.id] && <div className="qr-preview" dangerouslySetInnerHTML={{ __html: qrSvgs[access.id] }} />}
                {!isTerminal && <p className="muted">Подключение: импортируйте строку выше в VLESS/Xray клиент или используйте QR-код.</p>}
              </div>
            })}
          </div>
        </Card>

        <Card>
          <h3>Реферальные начисления</h3>
          <div className="list-stack">
            {referrals.length === 0 && <EmptyState title="Реферальных начислений нет" description="Когда появятся начисления, они будут показаны здесь." />}
            {referrals.map((reward) => (
              <div key={reward.id} className="list-item">
                <div>
                  <strong>{formatReferralRewardType(reward.type)}</strong>
                  <div className="muted">{reward.value} {reward.currencyOrUnit} · {new Date(reward.createdAt).toLocaleDateString()}</div>
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
