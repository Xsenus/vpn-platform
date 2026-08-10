import React, { useEffect, useMemo, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { ApiClient, AppReleaseDto, AppReleaseItemDto } from '@vpn-platform/api-client'
import { PrimaryButton } from '@vpn-platform/ui'

type AppVersionGateProps = {
  api: ApiClient
  token: string
  userId?: string | null
  manualOpenSignal: number
  onManualOpenHandled: () => void
}

const itemLabels: Record<string, string> = {
  new: 'Новое',
  improved: 'Улучшено',
  fixed: 'Исправлено',
  important: 'Важно'
}

const dialogFocusableSelector = [
  'a[href]',
  'button:not([disabled])',
  'input:not([disabled])',
  'select:not([disabled])',
  'textarea:not([disabled])',
  '[tabindex]:not([tabindex="-1"])'
].join(',')

function getDialogFocusableElements(dialog: HTMLElement) {
  return Array.from(dialog.querySelectorAll<HTMLElement>(dialogFocusableSelector))
    .filter((element) => element.getClientRects().length > 0 && element.getAttribute('aria-hidden') !== 'true')
}

function dismissedKey(userId: string, releaseId: string) {
  return `appVersion.dismissed.${userId}.${releaseId}`
}

function readDismissed(key: string) {
  try {
    return typeof localStorage !== 'undefined' && localStorage.getItem(key) === '1'
  } catch {
    return false
  }
}

function writeDismissed(key: string) {
  try {
    if (typeof localStorage !== 'undefined') localStorage.setItem(key, '1')
  } catch {
    // Local fallback is best-effort when browser storage is blocked.
  }
}

function formatReleaseDate(value: string) {
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? value
    : date.toLocaleString('ru-RU', { day: '2-digit', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function normalizeItemType(item: AppReleaseItemDto) {
  return itemLabels[item.type] ? item.type : 'new'
}

export function AppVersionGate({ api, token, userId, manualOpenSignal, onManualOpenHandled }: AppVersionGateProps) {
  const [latest, setLatest] = useState<AppReleaseDto | null>(null)
  const [history, setHistory] = useState<AppReleaseDto[]>([])
  const [selectedReleaseId, setSelectedReleaseId] = useState('')
  const [open, setOpen] = useState(false)
  const [loadingHistory, setLoadingHistory] = useState(false)
  const [historyAttempted, setHistoryAttempted] = useState(false)
  const [historyError, setHistoryError] = useState('')
  const sessionRequestIdRef = useRef(0)
  const latestRequestIdRef = useRef(0)
  const historyRequestIdRef = useRef(0)

  useEffect(() => {
    const sessionRequestId = ++sessionRequestIdRef.current
    latestRequestIdRef.current += 1
    historyRequestIdRef.current += 1
    setLatest(null)
    setHistory([])
    setSelectedReleaseId('')
    setLoadingHistory(false)
    setHistoryAttempted(false)
    setHistoryError('')
    setOpen(false)

    if (!token || !userId) return

    const latestRequestId = ++latestRequestIdRef.current
    const requestIsCurrent = () => sessionRequestIdRef.current === sessionRequestId
      && latestRequestIdRef.current === latestRequestId
    api.getLatestAppVersion(token)
      .then((response) => {
        if (!requestIsCurrent()) return
        const release = response.latestRelease ?? null
        setLatest(release)
        setSelectedReleaseId(release?.releaseId ?? '')
        if (release && !response.seenByCurrentUser && !readDismissed(dismissedKey(userId, release.releaseId))) {
          setOpen(true)
        }
      })
      .catch(() => {
        if (requestIsCurrent()) setLatest(null)
      })

    return () => {
      if (sessionRequestIdRef.current === sessionRequestId) sessionRequestIdRef.current += 1
      latestRequestIdRef.current += 1
      historyRequestIdRef.current += 1
    }
  }, [api, token, userId])

  useEffect(() => {
    if (!manualOpenSignal) return
    if (!token) {
      onManualOpenHandled()
      return
    }
    if (!userId) return

    setOpen(true)
    onManualOpenHandled()
    if (!latest) {
      const sessionRequestId = sessionRequestIdRef.current
      const latestRequestId = ++latestRequestIdRef.current
      const requestIsCurrent = () => sessionRequestIdRef.current === sessionRequestId
        && latestRequestIdRef.current === latestRequestId
      api.getLatestAppVersion(token)
        .then((response) => {
          if (!requestIsCurrent()) return
          const release = response.latestRelease ?? null
          setLatest(release)
          setSelectedReleaseId(release?.releaseId ?? '')
        })
        .catch(() => {
          if (requestIsCurrent()) setLatest(null)
        })
    }
  }, [api, latest, manualOpenSignal, onManualOpenHandled, token, userId])

  useEffect(() => {
    if (!open || !token || history.length > 0 || loadingHistory || historyAttempted) return

    const sessionRequestId = sessionRequestIdRef.current
    const historyRequestId = ++historyRequestIdRef.current
    const requestIsCurrent = () => sessionRequestIdRef.current === sessionRequestId
      && historyRequestIdRef.current === historyRequestId
    setHistoryAttempted(true)
    setHistoryError('')
    setLoadingHistory(true)
    api.getAppVersionHistory(token)
      .then((items) => {
        if (!requestIsCurrent()) return
        setHistory(items)
        setHistoryError('')
      })
      .catch(() => {
        if (!requestIsCurrent()) return
        setHistory([])
        setHistoryError('Не удалось загрузить историю обновлений.')
      })
      .finally(() => {
        if (requestIsCurrent()) setLoadingHistory(false)
      })
  }, [api, history.length, historyAttempted, loadingHistory, open, token])

  const retryHistory = () => {
    historyRequestIdRef.current += 1
    setHistory([])
    setHistoryError('')
    setLoadingHistory(false)
    setHistoryAttempted(false)
  }

  const selectedRelease = useMemo(() => {
    if (!selectedReleaseId) return latest
    return history.find((item) => item.releaseId === selectedReleaseId) ?? latest
  }, [history, latest, selectedReleaseId])

  const handleClose = async () => {
    setOpen(false)
    const release = latest
    if (!token || !userId || !release) return

    writeDismissed(dismissedKey(userId, release.releaseId))
    try {
      await api.markAppVersionSeen(token, release.releaseId)
    } catch {
      // The local dismissal prevents repeated auto-open while the next request retries server sync.
    }
  }

  if (!open || !selectedRelease) return null

  return (
    <AppVersionModal
      latestRelease={latest}
      selectedRelease={selectedRelease}
      history={history.length > 0 ? history : latest ? [latest] : []}
      loadingHistory={loadingHistory}
      historyError={historyError}
      onClose={() => void handleClose()}
      onRetryHistory={retryHistory}
      onSelectRelease={(releaseId) => setSelectedReleaseId(releaseId)}
      onShowCurrent={() => setSelectedReleaseId(latest?.releaseId ?? '')}
    />
  )
}

type AppVersionModalProps = {
  latestRelease: AppReleaseDto | null
  selectedRelease: AppReleaseDto
  history: AppReleaseDto[]
  loadingHistory: boolean
  historyError: string
  onClose: () => void
  onRetryHistory: () => void
  onSelectRelease: (releaseId: string) => void
  onShowCurrent: () => void
}

export function AppVersionModal({ latestRelease, selectedRelease, history, loadingHistory, historyError, onClose, onRetryHistory, onSelectRelease, onShowCurrent }: AppVersionModalProps) {
  const [historyOpen, setHistoryOpen] = useState(false)
  const dialogRef = useRef<HTMLElement | null>(null)
  const historyToggleRef = useRef<HTMLButtonElement | null>(null)
  const historyCloseRef = useRef<HTMLButtonElement | null>(null)
  const isCurrent = latestRelease?.releaseId === selectedRelease.releaseId

  useEffect(() => {
    const previousActiveElement = document.activeElement instanceof HTMLElement ? document.activeElement : null
    const appRoot = document.getElementById('root')
    const appRootWasInert = appRoot?.inert ?? false
    const previousBodyOverflow = document.body.style.overflow

    if (appRoot) appRoot.inert = true
    document.body.style.overflow = 'hidden'
    dialogRef.current?.focus()

    return () => {
      if (appRoot) appRoot.inert = appRootWasInert
      document.body.style.overflow = previousBodyOverflow
      if (previousActiveElement?.isConnected) previousActiveElement.focus()
    }
  }, [])

  const handleDialogKeyDown = (event: React.KeyboardEvent<HTMLElement>) => {
    if (event.key === 'Escape') {
      event.stopPropagation()
      onClose()
      return
    }

    if (event.key !== 'Tab' || !dialogRef.current) return
    const focusableElements = getDialogFocusableElements(dialogRef.current)
    if (focusableElements.length === 0) {
      event.preventDefault()
      dialogRef.current.focus()
      return
    }

    const firstElement = focusableElements[0]
    const lastElement = focusableElements[focusableElements.length - 1]
    const activeElement = document.activeElement
    if (event.shiftKey && (activeElement === dialogRef.current || activeElement === firstElement)) {
      event.preventDefault()
      lastElement.focus()
    } else if (!event.shiftKey && activeElement === lastElement) {
      event.preventDefault()
      firstElement.focus()
    }
  }

  const openHistory = () => {
    setHistoryOpen(true)
    window.requestAnimationFrame(() => historyCloseRef.current?.focus())
  }

  const closeHistory = () => {
    const restoreMobileToggle = Boolean(historyToggleRef.current?.getClientRects().length)
    setHistoryOpen(false)
    if (restoreMobileToggle) window.requestAnimationFrame(() => historyToggleRef.current?.focus())
  }

  return createPortal(
    <div className="app-version-overlay" role="presentation">
      <section
        ref={dialogRef}
        className="app-version-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="app-version-title"
        aria-describedby="app-version-summary"
        tabIndex={-1}
        onKeyDown={handleDialogKeyDown}
      >
        <button className="app-version-close" type="button" aria-label="Закрыть окно что нового" onClick={onClose}>×</button>
        <aside id="app-version-history" className={`app-version-history ${historyOpen ? 'is-open' : ''}`} aria-label="История обновлений">
          <div className="app-version-history-head">
            <strong>История обновлений</strong>
            <button ref={historyCloseRef} type="button" className="button button-ghost app-version-history-toggle" onClick={closeHistory}>Скрыть</button>
          </div>
          {loadingHistory && <p className="muted" role="status" aria-live="polite">Загружаем историю...</p>}
          {historyError && (
            <div className="app-version-history-error" role="alert">
              <p>{historyError}</p>
              <PrimaryButton type="button" className="button-secondary" onClick={onRetryHistory}>
                Повторить загрузку истории
              </PrimaryButton>
            </div>
          )}
          <div className="app-version-history-list">
            {history.map((release) => (
              <button
                key={release.releaseId}
                type="button"
                className={release.releaseId === selectedRelease.releaseId ? 'active' : ''}
                aria-current={release.releaseId === selectedRelease.releaseId ? 'true' : undefined}
                aria-label={`Версия ${release.version}, ${formatReleaseDate(release.releasedAt)}`}
                onClick={() => {
                  onSelectRelease(release.releaseId)
                  closeHistory()
                }}
              >
                <span>{release.version}</span>
                <small>{formatReleaseDate(release.releasedAt)}</small>
                {latestRelease?.releaseId === release.releaseId && <em>Текущее</em>}
              </button>
            ))}
          </div>
        </aside>
        <div className="app-version-content">
          <div className="app-version-mobile-actions">
            <button
              ref={historyToggleRef}
              type="button"
              className="button button-secondary"
              aria-expanded={historyOpen}
              aria-controls="app-version-history"
              onClick={openHistory}
            >
              История
            </button>
          </div>
          <header className="app-version-header">
            <div>
              <p className="eyebrow">Что нового</p>
              <h2 id="app-version-title">{selectedRelease.title}</h2>
              <p id="app-version-summary">{selectedRelease.summary}</p>
            </div>
            <div className="app-version-meta">
              <span>Версия {selectedRelease.version}</span>
              <small>{formatReleaseDate(selectedRelease.releasedAt)}</small>
              {!isCurrent && (
                <PrimaryButton type="button" className="button-secondary app-version-current" onClick={onShowCurrent}>
                  Показать текущее
                </PrimaryButton>
              )}
            </div>
          </header>
          <div className="app-version-items">
            {selectedRelease.items.map((item, index) => {
              const type = normalizeItemType(item)
              return (
                <article key={`${selectedRelease.releaseId}-${index}`} className={`app-version-item app-version-item-${type}`}>
                  <span>{itemLabels[type]}</span>
                  <p>{item.text}</p>
                </article>
              )
            })}
          </div>
        </div>
      </section>
    </div>,
    document.body
  )
}
