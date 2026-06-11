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

function dismissedKey(userId: string | null | undefined, releaseId: string) {
  return `appVersion.dismissed.${userId || 'anonymous'}.${releaseId}`
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

  useEffect(() => {
    if (!token) {
      setLatest(null)
      setHistory([])
      setOpen(false)
      return
    }

    let cancelled = false
    api.getLatestAppVersion(token)
      .then((response) => {
        if (cancelled) return
        const release = response.latestRelease ?? null
        setLatest(release)
        setSelectedReleaseId(release?.releaseId ?? '')
        if (release && !response.seenByCurrentUser && !readDismissed(dismissedKey(userId, release.releaseId))) {
          setOpen(true)
        }
      })
      .catch(() => {
        if (!cancelled) setLatest(null)
      })

    return () => {
      cancelled = true
    }
  }, [api, token, userId])

  useEffect(() => {
    if (!manualOpenSignal) return
    if (!token) {
      onManualOpenHandled()
      return
    }

    setOpen(true)
    onManualOpenHandled()
    if (!latest) {
      api.getLatestAppVersion(token)
        .then((response) => {
          const release = response.latestRelease ?? null
          setLatest(release)
          setSelectedReleaseId(release?.releaseId ?? '')
        })
        .catch(() => setLatest(null))
    }
  }, [api, latest, manualOpenSignal, onManualOpenHandled, token])

  useEffect(() => {
    if (!open || !token || history.length > 0 || loadingHistory) return

    setLoadingHistory(true)
    api.getAppVersionHistory(token)
      .then((items) => setHistory(items))
      .catch(() => setHistory([]))
      .finally(() => setLoadingHistory(false))
  }, [api, history.length, loadingHistory, open, token])

  const selectedRelease = useMemo(() => {
    if (!selectedReleaseId) return latest
    return history.find((item) => item.releaseId === selectedReleaseId) ?? latest
  }, [history, latest, selectedReleaseId])

  const handleClose = async () => {
    setOpen(false)
    const release = latest
    if (!token || !release) return

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
      onClose={() => void handleClose()}
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
  onClose: () => void
  onSelectRelease: (releaseId: string) => void
  onShowCurrent: () => void
}

export function AppVersionModal({ latestRelease, selectedRelease, history, loadingHistory, onClose, onSelectRelease, onShowCurrent }: AppVersionModalProps) {
  const [historyOpen, setHistoryOpen] = useState(false)
  const dialogRef = useRef<HTMLElement | null>(null)
  const isCurrent = latestRelease?.releaseId === selectedRelease.releaseId

  useEffect(() => {
    const previousActiveElement = document.activeElement instanceof HTMLElement ? document.activeElement : null
    dialogRef.current?.focus()

    return () => {
      previousActiveElement?.focus()
    }
  }, [])

  const handleDialogKeyDown = (event: React.KeyboardEvent<HTMLElement>) => {
    if (event.key !== 'Escape') return
    event.stopPropagation()
    onClose()
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
            <PrimaryButton type="button" className="button-ghost app-version-history-toggle" onClick={() => setHistoryOpen(false)}>Скрыть</PrimaryButton>
          </div>
          {loadingHistory && <p className="muted" role="status" aria-live="polite">Загружаем историю...</p>}
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
                  setHistoryOpen(false)
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
            <PrimaryButton
              type="button"
              className="button-secondary"
              aria-expanded={historyOpen}
              aria-controls="app-version-history"
              onClick={() => setHistoryOpen(true)}
            >
              История
            </PrimaryButton>
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
