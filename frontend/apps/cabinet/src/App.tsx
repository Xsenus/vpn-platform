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
  normalizeApiError,
  translateAuthError,
  translateAuthMessage,
  validateAuthInput,
  validatePasswordResetConfirm,
  validatePasswordResetRequest
} from '@vpn-platform/api-client'
import { Card, CodeBlock, CopyButton, EmptyState, ErrorBlock, ExternalLinkActions, LoadingBlock, PageShell, PasswordField, PrimaryButton, QrCodePreview, SegmentedTabs, SkipLink, StatTile, StatusBadge, ValidationModeBadge } from '@vpn-platform/ui'
import { AppVersionGate } from './AppVersion'
import { buildCabinetSummary, formatReferralRewardType, getAccessQrAvailability, getCabinetAccessTerminalReason, getSubscriptionRenewalAvailability } from './cabinet-dashboard'
import { cabinetSessionEndedMessage, isCabinetAccessTokenExpired, isCabinetSessionRejected } from './cabinet-session'
import { buildOrderExportText, formatPaymentMoney, getLatestPaymentForOrder, getOrderPaymentAvailability, getOrderStatusMessage, getPaymentStatusMessage, groupPaymentsByOrderId } from './cabinet-payments'
import { resolveCabinetPublicWebUrl } from './cabinet-public-url'
import { countOpenSupportConversations, getSupportStatusMessage, selectCurrentSupportConversation, validateSupportReply, validateSupportRequest } from './cabinet-support'

const api = new ApiClient(import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080')
const configuredPublicWebUrl = import.meta.env.VITE_PUBLIC_WEB_URL
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
  payment: PaymentInitResult | null
} | null

type RetryPaymentState = {
  order: OrderDto
  payment: PaymentInitResult
} | null

type CabinetActionContext = {
  operationId: number
  isCurrent: () => boolean
  reloadAll: () => Promise<boolean>
}

type CabinetActionRequest = {
  id: string
  operationId: number
}

type CabinetRequestContext = {
  operationId: number
  isCurrent: () => boolean
}

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
  const [sessionHydrating, setSessionHydrating] = useState(Boolean(token))
  const [logoutBusy, setLogoutBusy] = useState(false)
  const [authMode, setAuthMode] = useState<'login' | 'register'>('login')
  const [authEmail, setAuthEmail] = useState('')
  const [authPassword, setAuthPassword] = useState('')
  const [authDisplayName, setAuthDisplayName] = useState('')
  const [authReferralCode, setAuthReferralCode] = useState('')
  const [renewalState, setRenewalState] = useState<RenewalState>(null)
  const [retryPaymentState, setRetryPaymentState] = useState<RetryPaymentState>(null)
  const [qrSvgs, setQrSvgs] = useState<Record<string, string>>({})
  const [resetEmail, setResetEmail] = useState('')
  const [resetToken, setResetToken] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [appVersionOpenSignal, setAppVersionOpenSignal] = useState(0)
  const restoredSessionHydrationStarted = useRef(false)
  const sessionOperationId = useRef(0)
  const loadAllRequestId = useRef(0)
  const sessionActionInFlight = useRef<CabinetActionRequest | null>(null)
  const authActionInFlight = useRef<CabinetActionRequest | null>(null)
  const logoutInFlight = useRef(false)
  const supportMessagesRequestId = useRef(0)
  const supportMessagesEffectSkipId = useRef('')
  const paymentProvidersRequestId = useRef(0)
  const selectedSupportConversationIdRef = useRef(selectedSupportConversationId)
  const supportCreateDraftRef = useRef('')
  const supportReplyTextRef = useRef(supportReplyText)
  const authFormSnapshotRef = useRef('')
  const resetEmailRef = useRef(resetEmail)
  selectedSupportConversationIdRef.current = selectedSupportConversationId
  supportCreateDraftRef.current = JSON.stringify([supportSubject, supportText, supportOrderId, supportSubscriptionId])
  supportReplyTextRef.current = supportReplyText
  authFormSnapshotRef.current = JSON.stringify([authMode, authEmail, authPassword, authDisplayName, authReferralCode])
  resetEmailRef.current = resetEmail
  const renderSessionOperationId = sessionOperationId.current
  const authPanelId = 'cabinet-auth-panel'
  const activeAuthTabId = authMode === 'login' ? 'cabinet-auth-login-tab' : 'cabinet-auth-register-tab'
  const authValidationErrors = validateAuthInput(authMode, authEmail, authPassword, authDisplayName)
  const resetRequestErrors = validatePasswordResetRequest(resetEmail)
  const resetConfirmErrors = validatePasswordResetConfirm(resetToken, newPassword)
  const showAuthValidation = authValidationErrors.length > 0 && Boolean(authEmail || authPassword || authDisplayName)
  const showResetRequestValidation = Boolean(resetEmail)
  const showResetConfirmValidation = Boolean(resetToken || newPassword)
  const publicWebUrl = useMemo(
    () => resolveCabinetPublicWebUrl(configuredPublicWebUrl, typeof window === 'undefined' ? undefined : window.location),
    []
  )

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

  const selectSupportConversation = (conversationId: string) => {
    if (selectedSupportConversationIdRef.current === conversationId) return
    selectedSupportConversationIdRef.current = conversationId
    supportMessagesRequestId.current += 1
    setSupportMessages([])
    setSupportLoading(false)
    setSupportReplyText('')
    setSelectedSupportConversationId(conversationId)
  }

  const clearSession = () => {
    sessionOperationId.current += 1
    loadAllRequestId.current += 1
    sessionActionInFlight.current = null
    authActionInFlight.current = null
    logoutInFlight.current = false
    supportMessagesRequestId.current += 1
    supportMessagesEffectSkipId.current = ''
    paymentProvidersRequestId.current += 1
    setToken('')
    setRefreshToken('')
    setSessionHydrating(false)
    setBusy(false)
    setProfile(null)
    setSubscriptions([])
    setOrders([])
    setPayments([])
    setAccesses([])
    setReferrals([])
    setSupportConversations([])
    setSupportMessages([])
    selectedSupportConversationIdRef.current = ''
    setSelectedSupportConversationId('')
    setSupportSubject('')
    setSupportText('')
    setSupportReplyText('')
    setSupportOrderId('')
    setSupportSubscriptionId('')
    setSupportLoading(false)
    setTelegramStatus(null)
    setTelegramLink(null)
    setPaymentProviders([])
    setPaymentProvidersLoading(false)
    setPaymentProvidersError('')
    setProvider('')
    setRenewalState(null)
    setRetryPaymentState(null)
    setQrSvgs({})
    setAuthEmail('')
    setAuthPassword('')
    setAuthDisplayName('')
    setAuthReferralCode('')
    setResetEmail('')
    setResetToken('')
    setNewPassword('')
    removeSessionStorageItem(TOKEN_STORAGE_KEY)
    removeSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY)
  }

  const handleAuthenticatedError = (error: unknown, fallback: string) => {
    if (isCabinetSessionRejected(error)) {
      clearSession()
      setError(cabinetSessionEndedMessage)
      return
    }

    setError(normalizeApiError(error, fallback))
  }

  const storeSession = async (response: AuthResponse, operationId: number) => {
    if (sessionOperationId.current !== operationId) return false
    setToken(response.accessToken)
    setRefreshToken(response.refreshToken)
    writeSessionStorageItem(TOKEN_STORAGE_KEY, response.accessToken)
    writeSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY, response.refreshToken)
    return await loadAll(response.accessToken, { operationId })
  }

  const loadAll = async (currentToken: string, options?: { operationId?: number, throwOnError?: boolean }) => {
    if (!currentToken) return false

    const operationId = options?.operationId ?? renderSessionOperationId
    if (sessionOperationId.current !== operationId) return false
    const requestId = ++loadAllRequestId.current
    const operationIsCurrent = () => sessionOperationId.current === operationId
      && loadAllRequestId.current === requestId

    if (operationIsCurrent()) {
      setBusy(true)
      setError('')
    }

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

      if (!operationIsCurrent()) return false

      setProfile(nextProfile)
      setSubscriptions(nextSubscriptions)
      setOrders(nextOrders)
      setPayments(nextPayments)
      setAccesses(nextAccesses)
      setReferrals(nextReferrals)
      setSupportConversations(nextSupportConversations)
      const nextSelectedConversationId = nextSupportConversations.some((item) => item.id === selectedSupportConversationIdRef.current)
        ? selectedSupportConversationIdRef.current
        : nextSupportConversations[0]?.id ?? ''
      if (nextSelectedConversationId !== selectedSupportConversationIdRef.current) {
        selectSupportConversation(nextSelectedConversationId)
      }
      setTelegramStatus(nextTelegramStatus)
      return true
    } catch (e) {
      if (!operationIsCurrent()) return false
      if (options?.throwOnError) throw e
      handleAuthenticatedError(e, 'Не удалось загрузить кабинет')
      return false
    } finally {
      if (operationIsCurrent()) setBusy(false)
    }
  }

  const runSessionAction = async (
    id: string,
    fallback: string,
    action: (context: CabinetActionContext) => Promise<void>
  ) => {
    if (!token || sessionOperationId.current !== renderSessionOperationId || sessionActionInFlight.current) return false
    const request: CabinetActionRequest = { id, operationId: renderSessionOperationId }
    const isCurrent = () => sessionOperationId.current === request.operationId
      && sessionActionInFlight.current === request
    sessionActionInFlight.current = request
    setBusy(true)
    setError('')
    setNotice('')
    try {
      await action({
        operationId: request.operationId,
        isCurrent,
        reloadAll: () => isCurrent()
          ? loadAll(token, { operationId: request.operationId })
          : Promise.resolve(false)
      })
      return isCurrent()
    } catch (e) {
      if (isCurrent()) handleAuthenticatedError(e, fallback)
      return false
    } finally {
      if (sessionActionInFlight.current === request) sessionActionInFlight.current = null
      if (sessionOperationId.current === request.operationId) setBusy(false)
    }
  }

  const runAuthAction = async (
    id: string,
    startSessionOperation: boolean,
    onError: (error: unknown) => void,
    action: (context: CabinetRequestContext) => Promise<void>
  ) => {
    if (authActionInFlight.current || sessionActionInFlight.current) return false
    const operationId = startSessionOperation ? ++sessionOperationId.current : renderSessionOperationId
    if (sessionOperationId.current !== operationId) return false
    const request: CabinetActionRequest = { id, operationId }
    const isCurrent = () => sessionOperationId.current === operationId
      && authActionInFlight.current === request
    authActionInFlight.current = request
    setBusy(true)
    setError('')
    setNotice('')
    try {
      await action({ operationId, isCurrent })
      return isCurrent()
    } catch (e) {
      if (isCurrent()) onError(e)
      return false
    } finally {
      if (authActionInFlight.current === request) authActionInFlight.current = null
      if (sessionOperationId.current === operationId) setBusy(false)
    }
  }

  const hydrateRestoredSession = async (currentToken: string, currentRefreshToken: string) => {
    if (!currentToken) return false

    const operationId = ++sessionOperationId.current
    const operationIsCurrent = () => sessionOperationId.current === operationId
    setSessionHydrating(true)
    setError('')
    setNotice('')

    try {
      try {
        return await loadAll(currentToken, { operationId, throwOnError: true })
      } catch (e) {
        if (!operationIsCurrent()) return false
        if (!currentRefreshToken || !isCabinetAccessTokenExpired(e)) throw e

        const response = await api.refresh(currentRefreshToken)
        if (!operationIsCurrent()) return false

        setToken(response.accessToken)
        setRefreshToken(response.refreshToken)
        writeSessionStorageItem(TOKEN_STORAGE_KEY, response.accessToken)
        writeSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY, response.refreshToken)
        return await loadAll(response.accessToken, { operationId, throwOnError: true })
      }
    } catch (e) {
      if (!operationIsCurrent()) return false
      if (isCabinetSessionRejected(e)) {
        clearSession()
        setError(cabinetSessionEndedMessage)
      } else {
        setError(normalizeApiError(e, 'Не удалось восстановить сессию'))
      }
      return false
    } finally {
      if (operationIsCurrent()) setSessionHydrating(false)
    }
  }

  const loadSupportMessages = async (
    conversationId: string,
    currentToken = token,
    operationId = renderSessionOperationId
  ) => {
    if (!currentToken || !conversationId || sessionOperationId.current !== operationId || selectedSupportConversationIdRef.current !== conversationId) return false
    const requestId = ++supportMessagesRequestId.current
    const requestIsCurrent = () => sessionOperationId.current === operationId
      && supportMessagesRequestId.current === requestId
      && selectedSupportConversationIdRef.current === conversationId

    setSupportMessages([])
    setSupportLoading(true)
    try {
      const messages = await api.getMySupportMessages(currentToken, conversationId)
      if (!requestIsCurrent()) return false
      setSupportMessages(messages)
      return true
    } catch (e) {
      if (!requestIsCurrent()) return false
      handleAuthenticatedError(e, 'Не удалось загрузить переписку поддержки')
      return false
    } finally {
      if (requestIsCurrent()) setSupportLoading(false)
    }
  }

  useEffect(() => {
    if (restoredSessionHydrationStarted.current) return
    restoredSessionHydrationStarted.current = true
    // New login and refresh sessions are loaded by their handlers; this hydrates only a restored session.
    void hydrateRestoredSession(token, refreshToken)
  }, [])

  useEffect(() => {
    if (!token || !selectedSupportConversation?.id) {
      supportMessagesRequestId.current += 1
      setSupportMessages([])
      setSupportLoading(false)
      return
    }

    if (supportMessagesEffectSkipId.current === selectedSupportConversation.id) {
      supportMessagesEffectSkipId.current = ''
      return
    }

    void loadSupportMessages(selectedSupportConversation.id, token, sessionOperationId.current)
  }, [token, selectedSupportConversation?.id])

  useEffect(() => {
    const requestId = ++paymentProvidersRequestId.current
    const requestIsCurrent = () => paymentProvidersRequestId.current === requestId

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
        if (!requestIsCurrent()) return
        setPaymentProviders(items)
        setPaymentProvidersError('')
        setProvider((current) => current && items.some((item) => item.provider === current) ? current : (items[0]?.provider ?? ''))
      })
      .catch((e: unknown) => {
        if (!requestIsCurrent()) return
        setPaymentProviders([])
        setProvider('')
        setPaymentProvidersError(normalizeApiError(e, 'Не удалось загрузить способы оплаты.'))
      })
      .finally(() => {
        if (requestIsCurrent()) setPaymentProvidersLoading(false)
      })

    return () => {
      if (paymentProvidersRequestId.current === requestId) paymentProvidersRequestId.current += 1
    }
  }, [token])


  const handleAuthSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (authValidationErrors.length > 0) {
      setError(authValidationErrors.join(' '))
      return
    }

    const mode = authMode
    const email = authEmail.trim()
    const password = authPassword
    const displayName = authDisplayName.trim() || email
    const referralCode = authReferralCode
    const submittedForm = authFormSnapshotRef.current
    await runAuthAction(
      'auth-submit',
      true,
      (e) => setError(translateAuthError(e, mode === 'login' ? 'Не удалось войти' : 'Не удалось зарегистрироваться')),
      async (action) => {
        const response = mode === 'login'
          ? await api.login(email, password)
          : await api.register(email, password, displayName, referralCode)
        if (!action.isCurrent() || !await storeSession(response, action.operationId)) return
        if (authFormSnapshotRef.current === submittedForm) {
          setAuthPassword('')
          setAuthReferralCode('')
        }
        setNotice(mode === 'login' ? 'Вход выполнен.' : 'Аккаунт создан.')
      }
    )
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
    const currentRefreshToken = refreshToken
    await runAuthAction(
      'refresh-session',
      true,
      (e) => handleAuthenticatedError(e, 'Не удалось обновить сессию'),
      async (action) => {
        const response = await api.refresh(currentRefreshToken)
        if (!action.isCurrent()) return
        setToken(response.accessToken)
        setRefreshToken(response.refreshToken)
        writeSessionStorageItem(TOKEN_STORAGE_KEY, response.accessToken)
        writeSessionStorageItem(REFRESH_TOKEN_STORAGE_KEY, response.refreshToken)
        if (!await loadAll(response.accessToken, { operationId: action.operationId })) return
        setNotice('Сессия обновлена.')
      }
    )
  }

  const handleLogout = async () => {
    if (logoutBusy || logoutInFlight.current) return
    logoutInFlight.current = true
    sessionOperationId.current += 1
    setSessionHydrating(false)
    setLogoutBusy(true)
    setBusy(true)
    setError('')
    setNotice('')
    try {
      await api.logout(token || null, refreshToken || null)
    } catch {
      setError('Локальная сессия завершена, но отзыв серверной сессии не подтверждён. На чужом устройстве измените пароль из доверенного браузера.')
    } finally {
      clearSession()
      logoutInFlight.current = false
      setBusy(false)
      setLogoutBusy(false)
    }
  }

  const handleForgotPassword = async () => {
    if (resetRequestErrors.length > 0) {
      setError(resetRequestErrors.join(' '))
      return
    }

    const email = resetEmail
    await runAuthAction(
      'forgot-password',
      false,
      (e) => setError(translateAuthError(e, 'Не удалось запросить сброс пароля')),
      async (action) => {
        const response = await api.forgotPassword(email)
        if (!action.isCurrent()) return
        if (response.validationResetToken && resetEmailRef.current === email) setResetToken(response.validationResetToken)
        setNotice(translateAuthMessage(response.message))
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
    await runAuthAction(
      'reset-password',
      false,
      (e) => setError(translateAuthError(e, 'Не удалось сбросить пароль')),
      async (action) => {
        await api.resetPassword(submittedToken, submittedPassword)
        if (!action.isCurrent()) return
        clearSession()
        setNotice('Пароль изменён. Войдите с новым паролем.')
      }
    )
  }

  const handleLoadQr = async (accessId: string) => {
    if (!token) return
    const clearCachedQr = () => setQrSvgs((current) => {
      if (!current[accessId]) return current
      const next = { ...current }
      delete next[accessId]
      return next
    })
    const qrAvailability = getAccessQrAvailability(accesses.find((access) => access.id === accessId))
    if (!qrAvailability.canGenerate) {
      clearCachedQr()
      setError(qrAvailability.reason ?? 'QR-код пока недоступен.')
      return
    }
    await runSessionAction(`qr-${accessId}`, 'Не удалось загрузить QR', async (action) => {
      clearCachedQr()
      const svg = await api.getMyAccessQrSvg(token, accessId)
      if (!action.isCurrent()) return
      setQrSvgs((current) => ({ ...current, [accessId]: svg }))
      setNotice('QR-код загружен.')
    })
  }

  const handleCreateTelegramLink = async () => {
    if (!token) return
    await runSessionAction('telegram-link', 'Не удалось создать ссылку Telegram', async (action) => {
      const link = await api.createTelegramLinkToken(token)
      if (!action.isCurrent()) return
      setTelegramLink(link)
      if (!await action.reloadAll()) return
      setNotice('Ссылка на Telegram-бота создана.')
    })
  }

  const handleUnlinkTelegram = async () => {
    if (!token) return
    await runSessionAction('telegram-unlink', 'Не удалось отвязать Telegram', async (action) => {
      const status = await api.unlinkTelegram(token)
      if (!action.isCurrent()) return
      setTelegramStatus(status)
      setTelegramLink(null)
      setNotice('Telegram отвязан от аккаунта.')
    })
  }

  const handleCreateSupportConversation = async () => {
    if (!token) return
    const validationErrors = validateSupportRequest(supportSubject, supportText)
    if (validationErrors.length > 0) {
      setError(validationErrors.join(' '))
      return
    }

    const submittedDraft = supportCreateDraftRef.current
    const payload = {
      subject: supportSubject.trim(),
      text: supportText.trim(),
      orderId: supportOrderId || null,
      subscriptionId: supportSubscriptionId || null
    }
    await runSessionAction('support-create', 'Не удалось создать обращение в поддержку', async (action) => {
      const conversation = await api.createMySupportConversation(token, {
        ...payload
      })
      if (!action.isCurrent()) return
      setSupportConversations((current) => [conversation, ...current.filter((item) => item.id !== conversation.id)])
      if (selectedSupportConversationIdRef.current !== conversation.id) {
        supportMessagesEffectSkipId.current = conversation.id
      }
      selectSupportConversation(conversation.id)
      if (supportCreateDraftRef.current === submittedDraft) {
        setSupportSubject('')
        setSupportText('')
        setSupportOrderId('')
        setSupportSubscriptionId('')
      }
      if (!await loadSupportMessages(conversation.id, token, action.operationId)) return
      setNotice('Обращение в поддержку создано.')
    })
  }

  const handleReplySupportConversation = async () => {
    if (!token || !selectedSupportConversation) return
    const validationErrors = validateSupportReply(supportReplyText)
    if (validationErrors.length > 0) {
      setError(validationErrors.join(' '))
      return
    }

    const conversation = selectedSupportConversation
    const submittedReply = supportReplyText
    await runSessionAction(`support-reply-${conversation.id}`, 'Не удалось отправить сообщение в поддержку', async (action) => {
      let message: SupportMessageDto
      try {
        message = await api.replyMySupportConversation(token, conversation.id, submittedReply.trim(), conversation.revision)
      } catch (e) {
        if (!action.isCurrent()) return
        if (e instanceof ApiClientError && e.status === 409) {
          await action.reloadAll()
          const currentConversationId = selectedSupportConversationIdRef.current
          if (action.isCurrent() && currentConversationId) {
            await loadSupportMessages(currentConversationId, token, action.operationId)
          }
        }
        throw e
      }
      if (!action.isCurrent()) return
      if (selectedSupportConversationIdRef.current === conversation.id) {
        setSupportMessages((current) => [...current, message])
        if (supportReplyTextRef.current === submittedReply) setSupportReplyText('')
      }
      setSupportConversations((current) => current.map((item) => item.id === conversation.id ? { ...item, status: 'open', revision: item.revision + 1, updatedAt: new Date().toISOString(), closedAt: null } : item))
      setNotice('Сообщение отправлено в поддержку.')
    })
  }

  const handleSupportConversationStatus = async (status: 'open' | 'closed') => {
    if (!token || !selectedSupportConversation) return
    const conversation = selectedSupportConversation
    await runSessionAction(`support-status-${conversation.id}`, 'Не удалось изменить статус обращения', async (action) => {
      let result: { status: string; revision: number }
      try {
        result = await api.updateMySupportConversationStatus(token, conversation.id, status, conversation.revision)
      } catch (e) {
        if (!action.isCurrent()) return
        if (e instanceof ApiClientError && e.status === 409) {
          await action.reloadAll()
          const currentConversationId = selectedSupportConversationIdRef.current
          if (action.isCurrent() && currentConversationId) {
            await loadSupportMessages(currentConversationId, token, action.operationId)
          }
        }
        throw e
      }
      if (!action.isCurrent()) return
      setSupportConversations((current) => current.map((item) => item.id === conversation.id ? { ...item, status: result.status, revision: result.revision, closedAt: result.status === 'closed' ? new Date().toISOString() : null, updatedAt: new Date().toISOString() } : item))
      setNotice(result.status === 'closed' ? 'Обращение закрыто.' : 'Обращение переоткрыто.')
    })
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
    const selectedProvider = provider
    await runSessionAction(`payment-retry-${order.id}`, 'Не удалось повторить оплату', async (action) => {
      const payment = await api.initMyPayment(token, order.id, selectedProvider, window.location.origin)
      if (!action.isCurrent()) return
      setRetryPaymentState({ order, payment })
      if (!await action.reloadAll()) return
      setNotice('Ссылка для повторной оплаты подготовлена.')
    })
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

    const selectedProvider = provider
    await runSessionAction(`renew-${subscription.id}`, 'Не удалось создать продление', async (action) => {
      let createdOrder: OrderDto | null = null
      try {
        const order = await api.createMyOrder(token, {
        tariffId: subscription.tariffId,
        type: 'Renewal',
        paymentProvider: selectedProvider,
        promoCode: null,
        subscriptionId: subscription.id
        })
        if (!action.isCurrent()) return
        createdOrder = order
        setOrders((current) => [order, ...current.filter((item) => item.id !== order.id)])
        setRenewalState({ subscriptionId: subscription.id, order, payment: null })
        const payment = await api.initMyPayment(token, order.id, selectedProvider, window.location.origin)
        if (!action.isCurrent()) return
        setRenewalState({ subscriptionId: subscription.id, order, payment })
        if (!await action.reloadAll()) return
        setNotice('Продление создано. Откройте оплату в карточке последнего продления.')
      } catch (e) {
        if (!action.isCurrent()) return
        if (createdOrder && !isCabinetSessionRejected(e)) {
          const normalizedDetails = normalizeApiError(e, '').trim()
          const details = normalizedDetails ? ` ${normalizedDetails}` : ''
          setError(`Заказ на продление ${createdOrder.id} создан, но ссылку оплаты подготовить не удалось. Повторите подготовку оплаты для этого заказа.${details}`)
          return
        }
        throw e
      }
    })
  }

  const handleRetryRenewalPayment = async () => {
    if (!token || !provider || !renewalState || renewalState.payment) return
    const paymentAvailability = getOrderPaymentAvailability(renewalState.order)
    if (!paymentAvailability.canRetry) {
      setError(paymentAvailability.reason ?? 'Этот заказ нельзя оплатить повторно. Создайте новый заказ.')
      return
    }

    const renewal = renewalState
    const selectedProvider = provider
    await runSessionAction(`renewal-payment-${renewal.order.id}`, 'Не удалось подготовить оплату продления', async (action) => {
      try {
        const payment = await api.initMyPayment(token, renewal.order.id, selectedProvider, window.location.origin)
        if (!action.isCurrent()) return
        setRenewalState((current) => current && current.order.id === renewal.order.id
        ? { ...current, payment }
        : current)
        if (!await action.reloadAll()) return
        setNotice('Ссылка оплаты для созданного продления подготовлена.')
      } catch (e) {
        if (!action.isCurrent()) return
        if (isCabinetSessionRejected(e)) throw e
        const normalizedDetails = normalizeApiError(e, '').trim()
        const details = normalizedDetails ? ` ${normalizedDetails}` : ''
        setError(`Заказ на продление ${renewal.order.id} сохранён, но ссылку оплаты снова подготовить не удалось.${details}`)
      }
    })
  }

  return (
    <>
      <SkipLink />
      <header className="topbar">
        <a className="app-brand" href={publicWebUrl}>VPN Platform</a>
        <nav aria-label="Основная навигация">
          <a href={`${publicWebUrl}/tariffs`}>Тарифы</a>
          <a href={`${publicWebUrl}/help`}>Помощь</a>
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

      {profile && (
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
                      {currentAccessId && qrSvgs[currentAccessId] && <QrCodePreview svg={qrSvgs[currentAccessId]} />}
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
              <div id={authPanelId} role="tabpanel" aria-labelledby={activeAuthTabId}>
                <form aria-busy={busy} onSubmit={(event) => void handleAuthSubmit(event)}>
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
              </div>
            </>
          ) : !profile ? (
            <>
              <h3>Восстановление сессии</h3>
              <p className="muted">
                {sessionHydrating
                  ? 'Проверяем сохранённую сессию и загружаем данные кабинета.'
                  : 'Не удалось загрузить данные кабинета. Сессия сохранена, можно повторить попытку.'}
              </p>
              <div className="toolbar">
                <PrimaryButton disabled={sessionHydrating || logoutBusy} aria-busy={sessionHydrating} onClick={() => void hydrateRestoredSession(token, refreshToken)}>Повторить загрузку</PrimaryButton>
                <PrimaryButton disabled={logoutBusy} aria-busy={logoutBusy} className="button-secondary" onClick={() => void handleLogout()}>Выйти</PrimaryButton>
              </div>
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
                <PrimaryButton disabled={logoutBusy} aria-busy={logoutBusy} className="button-secondary" onClick={() => void handleLogout()}>Выйти</PrimaryButton>
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
                    <ExternalLinkActions
                      value={telegramLink.deepLinkUrl}
                      openLabel="Открыть бота"
                      ariaLabel="Открыть Telegram-бота в новой вкладке"
                      invalidMessage="Ссылка на Telegram-бота отклонена как некорректная. Создайте новую ссылку."
                    />
                  </>
                )}
              </>
            )}
          </Card>

          {renewalState && (
            <Card>
              <h3>Последнее продление</h3>
              <p>ID подписки: {renewalState.subscriptionId}</p>
              <p>ID заказа: {renewalState.order.id}</p>
              <p>Статус заказа: <StatusBadge value={renewalState.order.status} /></p>
              {renewalState.payment ? (
                <>
                  <p>ID платежа: {renewalState.payment.paymentId}</p>
                  <ExternalLinkActions
                    value={renewalState.payment.redirectUrl}
                    openLabel="Открыть оплату"
                    ariaLabel="Открыть оплату в новой вкладке"
                    invalidMessage="Ссылка оплаты отклонена как некорректная. Повторите продление или обратитесь в поддержку."
                  />
                </>
              ) : (
                <>
                  <p className="safe-note">Заказ сохранён, но ссылка оплаты ещё не подготовлена. Повторная команда использует этот же заказ.</p>
                  <PrimaryButton type="button" onClick={handleRetryRenewalPayment} disabled={busy}>
                    Повторить подготовку оплаты
                  </PrimaryButton>
                </>
              )}
            </Card>
          )}

          {retryPaymentState && (
            <Card>
              <h3>Последняя повторная оплата</h3>
              <p>Заказ: {retryPaymentState.order.tariffName || retryPaymentState.order.id}</p>
              <p>ID платежа: {retryPaymentState.payment.paymentId}</p>
              <ExternalLinkActions
                value={retryPaymentState.payment.redirectUrl}
                openLabel="Открыть оплату"
                ariaLabel="Открыть повторную оплату в новой вкладке"
                invalidMessage="Ссылка повторной оплаты отклонена как некорректная. Повторите операцию или обратитесь в поддержку."
              />
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
              {!isTerminal && subscription.currentAccessId && qrSvgs[subscription.currentAccessId] && <QrCodePreview svg={qrSvgs[subscription.currentAccessId]} />}
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
              {!isTerminal && qrSvgs[access.id] && <QrCodePreview svg={qrSvgs[access.id]} />}
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
                  <ExternalLinkActions
                    value={payment.confirmationUrl}
                    openLabel="Открыть оплату"
                    className="toolbar compact"
                    showValue={false}
                    invalidMessage="Сохраненная ссылка оплаты отклонена как некорректная. Создайте новую попытку оплаты или обратитесь в поддержку."
                  />
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
                    onClick={() => selectSupportConversation(conversation.id)}
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
                {!isTerminal && qrSvgs[access.id] && <QrCodePreview svg={qrSvgs[access.id]} />}
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
