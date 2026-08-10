import React, { PropsWithChildren, useEffect, useId, useRef, useState } from 'react'

export const designTokens = {
  colors: {
    bg: 'var(--bg)',
    surface: 'var(--surface)',
    surfaceSoft: 'var(--surface-soft)',
    line: 'var(--line)',
    lineStrong: 'var(--line-strong)',
    text: 'var(--text)',
    muted: 'var(--muted)',
    primary: 'var(--primary)',
    primaryStrong: 'var(--primary-strong)',
    danger: 'var(--danger)',
    warning: 'var(--warning)',
    info: 'var(--info)'
  },
  radius: {
    sm: 'var(--radius-sm)',
    md: 'var(--radius-md)',
    pill: 'var(--radius-pill)'
  },
  shadow: {
    surface: 'var(--shadow)'
  }
} as const

export function PageShell({ title, children }: PropsWithChildren<{ title: string }>) {
  return (
    <main id="main-content" className="page-shell" tabIndex={-1}>
      <h1 className="page-title">{title}</h1>
      {children}
    </main>
  )
}

export function SkipLink({ href = '#main-content', label = 'Перейти к содержимому', updateHash = true }: { href?: string; label?: string; updateHash?: boolean }) {
  const activate = (event: React.MouseEvent<HTMLAnchorElement> | React.KeyboardEvent<HTMLAnchorElement>) => {
    if (!href.startsWith('#')) return

    const target = document.querySelector<HTMLElement>(href)
    if (!target) return

    event.preventDefault()
    target.focus({ preventScroll: true })
    target.scrollIntoView({ block: 'start' })
    if (updateHash) window.history.replaceState(null, '', href)
  }

  const handleKeyDown = (event: React.KeyboardEvent<HTMLAnchorElement>) => {
    if (event.key !== 'Enter' && event.key !== ' ') return
    activate(event)
  }

  return <a className="skip-link" href={href} onClick={activate} onKeyDown={handleKeyDown}>{label}</a>
}

export function Card({ children, className, style, ...props }: PropsWithChildren<React.HTMLAttributes<HTMLDivElement>>) {
  return (
    <div {...props} className={['card', className].filter(Boolean).join(' ')} style={style}>
      {children}
    </div>
  )
}

export function SectionCard({ title, description, children, actions }: PropsWithChildren<{ title: string; description?: string; actions?: React.ReactNode }>) {
  return (
    <Card>
      <div className="section-header">
        <div>
          <h3>{title}</h3>
          {description && <p className="muted">{description}</p>}
        </div>
        {actions && <div className="actions">{actions}</div>}
      </div>
      {children}
    </Card>
  )
}

export function StatTile({ label, value, hint }: { label: string; value: string | number; hint?: string }) {
  return (
    <Card className="stat-tile">
      <div className="stat-label">{label}</div>
      <div className="stat-value">{value}</div>
      {hint && <div className="stat-hint">{hint}</div>}
    </Card>
  )
}

function badgeTone(value: unknown) {
  const normalized = String(value ?? 'Unknown').toLowerCase()
  if (normalized.includes('active') || normalized.includes('ready') || normalized.includes('paid') || normalized.includes('healthy') || normalized.includes('enabled') || normalized.includes('completed') || normalized.includes('deployed') || normalized.includes('linked') || normalized.includes('published') || normalized.includes('home')) return 'success'
  if (normalized.includes('fail') || normalized.includes('error') || normalized.includes('blocked') || normalized.includes('cancel') || normalized.includes('disabled') || normalized.includes('unhealthy') || normalized.includes('hidden')) return 'danger'
  if (normalized.includes('pending') || normalized.includes('queued') || normalized.includes('grace') || normalized.includes('progress') || normalized.includes('sandbox') || normalized.includes('validation') || normalized.includes('attention') || normalized.includes('upcoming') || normalized.includes('проблем')) return 'warning'
  return 'neutral'
}

const statusLabels: Record<string, string> = {
  Active: 'Активно',
  Ready: 'Готово',
  Attention: 'Нужно внимание',
  Published: 'Опубликовано',
  Hidden: 'Скрыто',
  Upcoming: 'Запланировано',
  Home: 'Главная',
  agent: 'Агент',
  manual: 'Вручную',
  auto: 'Автоматически',
  hybrid: 'Гибридный',
  LongPolling: 'Опрос Telegram',
  Webhook: 'Webhook',
  Paid: 'Оплачено',
  Healthy: 'Работает',
  Enabled: 'Включено',
  Completed: 'Завершено',
  Deployed: 'Развернуто',
  Linked: 'Привязано',
  NotLinked: 'Не привязано',
  Failed: 'Ошибка',
  Error: 'Ошибка',
  Blocked: 'Заблокировано',
  Cancelled: 'Отменено',
  Canceled: 'Отменено',
  Disabled: 'Выключено',
  Unhealthy: 'Проблема',
  Pending: 'Ожидает',
  Approved: 'Подтверждено',
  Reverted: 'Отозвано',
  PendingPayment: 'Ожидает оплаты',
  PaymentReceived: 'Оплата получена',
  FulfillmentInProgress: 'Выдача',
  PartiallyProcessed: 'Частично обработано',
  PartiallyRefunded: 'Частичный возврат',
  Refunded: 'Возвращено',
  'Refund ready': 'Возврат доступен',
  'Refund blocked': 'Возврат недоступен',
  WaitingConfirmation: 'Ждет подтверждения',
  Queued: 'В очереди',
  GracePeriod: 'Льготный период',
  InProgress: 'В работе',
  Sandbox: 'Проверка',
  Validation: 'Проверка',
  Unknown: 'Неизвестно',
  Configured: 'Задано',
  'Write-only': 'Скрыто',
  Default: 'Основной',
  Inactive: 'Неактивно',
  New: 'Новый',
  Suspended: 'Ограничен',
  Deleted: 'Удален',
  Open: 'Открыто',
  Closed: 'Закрыто',
  'Checkout ready': 'Готово к оплате',
  'Not configured': 'Не настроено',
  'Access linked': 'Доступ привязан',
  'No access': 'Доступа нет'
}

function badgeLabel(value: unknown) {
  const raw = String(value ?? 'Unknown')
  return statusLabels[raw] ?? raw
}

export function StatusBadge({ value }: { value: unknown }) {
  const raw = String(value ?? 'Unknown')
  const label = badgeLabel(raw)
  const resolvedLabel = label || 'Неизвестно'
  return (
    <span className={`status-badge status-badge-${badgeTone(raw)}`} role="status" aria-label={`Статус: ${resolvedLabel}`}>
      {resolvedLabel}
    </span>
  )
}

export function CodeBlock({ children }: PropsWithChildren) {
  return <div className="code-block">{children}</div>
}

export function PrimaryButton({ type = 'button', ...props }: React.ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <button
      {...props}
      type={type}
      className={['button', props.className].filter(Boolean).join(' ')}
      style={props.style}
    />
  )
}

export type SegmentedTabOption<TValue extends string = string> = {
  value: TValue
  label: string
  disabled?: boolean
}

export function SegmentedTabs<TValue extends string = string>({
  options,
  value,
  onChange,
  idPrefix,
  panelId,
  label
}: {
  options: Array<SegmentedTabOption<TValue>>
  value: TValue
  onChange: (value: TValue) => void
  idPrefix: string
  panelId: string
  label: string
}) {
  const enabledOptions = options.filter((option) => !option.disabled)
  const focusTab = (nextValue: TValue) => {
    if (typeof document === 'undefined') return
    document.getElementById(`${idPrefix}-${nextValue}-tab`)?.focus()
  }

  const handleKeyDown = (event: React.KeyboardEvent<HTMLDivElement>) => {
    if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key) || enabledOptions.length === 0) return
    event.preventDefault()
    const currentIndex = Math.max(0, enabledOptions.findIndex((option) => option.value === value))
    const nextIndex = event.key === 'Home'
      ? 0
      : event.key === 'End'
        ? enabledOptions.length - 1
        : event.key === 'ArrowRight'
          ? (currentIndex + 1) % enabledOptions.length
          : (currentIndex - 1 + enabledOptions.length) % enabledOptions.length
    const nextValue = enabledOptions[nextIndex].value
    onChange(nextValue)
    if (typeof window !== 'undefined' && window.requestAnimationFrame) {
      window.requestAnimationFrame(() => focusTab(nextValue))
    } else {
      focusTab(nextValue)
    }
  }

  return (
    <div className="segmented-control" role="tablist" aria-label={label} aria-orientation="horizontal" onKeyDown={handleKeyDown}>
      {options.map((option) => (
        <PrimaryButton
          key={option.value}
          id={`${idPrefix}-${option.value}-tab`}
          type="button"
          role="tab"
          disabled={option.disabled}
          className={option.value === value ? 'active' : ''}
          aria-selected={option.value === value}
          aria-controls={panelId}
          tabIndex={option.value === value ? 0 : -1}
          onClick={() => onChange(option.value)}
        >
          {option.label}
        </PrimaryButton>
      ))}
    </div>
  )
}

export function EmptyState({ title, description, action }: { title: string; description?: string; action?: React.ReactNode }) {
  return (
    <div className="empty-state">
      <strong>{title}</strong>
      {description && <span>{description}</span>}
      {action && <div>{action}</div>}
    </div>
  )
}

export function LoadingBlock({ label = 'Загрузка...' }: { label?: string }) {
  return (
    <div role="status" aria-live="polite" aria-busy="true" className="loading-block">
      {label}
    </div>
  )
}

export function ErrorBlock({ message }: { message: string }) {
  if (!message) return null
  return (
    <div role="alert" className="error-block">
      {message}
    </div>
  )
}

type ClipboardWriter = Pick<Clipboard, 'writeText'>

export function getSafeHttpUrl(value?: string | null) {
  const normalized = String(value ?? '').trim()
  if (!normalized) return null

  try {
    const url = new URL(normalized)
    if ((url.protocol !== 'http:' && url.protocol !== 'https:') || url.username || url.password || !url.hostname) return null
    return normalized
  } catch {
    return null
  }
}

export async function tryWriteClipboardText(value: string, writer?: ClipboardWriter | null) {
  try {
    const clipboard = writer === undefined
      ? (typeof navigator === 'undefined' ? null : navigator.clipboard)
      : writer
    if (!clipboard?.writeText) return false
    await clipboard.writeText(value)
    return true
  } catch {
    return false
  }
}

export function CopyButton({ value, label = 'Скопировать', disabled }: { value?: string | null; label?: string; disabled?: boolean }) {
  const feedbackId = useId()
  const [status, setStatus] = useState<'idle' | 'copied' | 'failed'>('idle')
  const [busy, setBusy] = useState(false)
  const inFlightRef = useRef(false)
  const operationRef = useRef(0)
  const resetTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const canCopy = Boolean(value) && !disabled

  useEffect(() => () => {
    inFlightRef.current = false
    operationRef.current += 1
    if (resetTimerRef.current) clearTimeout(resetTimerRef.current)
  }, [])

  const handleCopy = async () => {
    if (!value || inFlightRef.current) return
    inFlightRef.current = true
    const operation = ++operationRef.current
    if (resetTimerRef.current) clearTimeout(resetTimerRef.current)
    setStatus('idle')
    setBusy(true)
    const copied = await tryWriteClipboardText(value)
    if (operation !== operationRef.current) return
    inFlightRef.current = false
    setStatus(copied ? 'copied' : 'failed')
    setBusy(false)
    resetTimerRef.current = setTimeout(() => {
      if (operation === operationRef.current) setStatus('idle')
    }, 1800)
  }

  const feedback = status === 'copied'
    ? 'Скопировано'
    : status === 'failed'
      ? 'Не удалось скопировать'
      : '\u00a0'

  return (
    <span className="copy-action">
      <PrimaryButton
        type="button"
        disabled={!canCopy || busy}
        aria-busy={busy}
        aria-describedby={feedbackId}
        onClick={() => void handleCopy()}
        title={value ? 'Скопировать в буфер обмена' : 'Нечего копировать'}
        aria-label={value ? `${label}: скопировать значение в буфер обмена` : `${label}: нечего копировать`}
        className="button-secondary"
      >
        {label}
      </PrimaryButton>
      <span id={feedbackId} className={`copy-feedback${status === 'failed' ? ' copy-feedback-error' : ''}`} role="status" aria-live="polite">{feedback}</span>
    </span>
  )
}

export function ExternalLinkActions({
  value,
  openLabel,
  copyLabel = 'Скопировать ссылку',
  ariaLabel,
  invalidMessage = 'Внешняя ссылка недоступна: получен некорректный адрес.',
  className = 'copy-row',
  valueClassName,
  showValue = true
}: {
  value?: string | null
  openLabel: string
  copyLabel?: string | null
  ariaLabel?: string
  invalidMessage?: string
  className?: string
  valueClassName?: string
  showValue?: boolean
}) {
  const safeUrl = getSafeHttpUrl(value)
  if (!safeUrl) return <p className="safe-note external-link-warning" role="alert">{invalidMessage}</p>

  return (
    <>
      <div className={className}>
        <a href={safeUrl} target="_blank" rel="noopener noreferrer" className="button" aria-label={ariaLabel}>{openLabel}</a>
        {copyLabel && <CopyButton value={safeUrl} label={copyLabel} />}
      </div>
      {showValue && <div className={valueClassName}><CodeBlock>{safeUrl}</CodeBlock></div>}
    </>
  )
}

export function getSafeSvgImageDataUrl(content?: string | null) {
  const normalized = String(content ?? '').trim()
  if (!normalized || normalized.length > 1_000_000) return null

  const svgRoot = normalized.replace(/^<\?xml[^>]*>\s*/i, '')
  if (!/^<svg(?:\s|>)/i.test(svgRoot) || !/<\/svg>\s*$/i.test(svgRoot)) return null
  if (/<\s*(?:script|foreignObject|iframe|object|embed|image|use|animate|set|audio|video|style|link)\b/i.test(svgRoot)) return null
  if (/(?:^|\s)on[a-z]+\s*=|(?:href|xlink:href|style)\s*=|<!doctype|<!entity/i.test(svgRoot)) return null

  return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(normalized)}`
}

export function QrCodePreview({ svg, label = 'QR-код VPN-доступа' }: { svg?: string | null; label?: string }) {
  const src = getSafeSvgImageDataUrl(svg)
  if (!src) return <p className="safe-note qr-preview-error" role="alert">QR-код отклонен: сервер вернул неподдерживаемое SVG-изображение.</p>

  return (
    <div className="qr-preview">
      <img src={src} alt={label} width={220} height={220} decoding="async" />
    </div>
  )
}

export function PasswordField({
  label,
  value,
  onChange,
  placeholder,
  autoComplete,
  minLength,
  required,
  help
}: {
  label: string
  value: string
  onChange: (value: string) => void
  placeholder?: string
  autoComplete?: string
  minLength?: number
  required?: boolean
  help?: string
}) {
  const inputId = useId()
  const helpId = useId()
  const [visible, setVisible] = useState(false)

  return (
    <div className="form-field">
      <label htmlFor={inputId}>{label}</label>
      <span className="password-field-row">
        <input
          id={inputId}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
          type={visible ? 'text' : 'password'}
          autoComplete={autoComplete}
          minLength={minLength}
          required={required}
          aria-describedby={help ? helpId : undefined}
        />
        <PrimaryButton
          type="button"
          className="button-ghost password-toggle"
          aria-label={visible ? `Скрыть ${label.toLowerCase()}` : `Показать ${label.toLowerCase()}`}
          aria-pressed={visible}
          title={visible ? 'Скрыть значение' : 'Показать значение'}
          onClick={() => setVisible((current) => !current)}
        >
          {visible ? 'Скрыть' : 'Показать'}
        </PrimaryButton>
      </span>
      {help && <small id={helpId}>{help}</small>}
    </div>
  )
}

export function ConfirmButton({ message, onConfirm, children, disabled, style, className }: PropsWithChildren<{ message: string; onConfirm: () => void | Promise<void>; disabled?: boolean; style?: React.CSSProperties; className?: string }>) {
  const panelId = useId()
  const messageId = `${panelId}-message`
  const [open, setOpen] = useState(false)
  const [busy, setBusy] = useState(false)

  const close = () => {
    if (!busy) setOpen(false)
  }

  const handleKeyDown = (event: React.KeyboardEvent<HTMLSpanElement>) => {
    if (event.key !== 'Escape') return
    event.stopPropagation()
    close()
  }

  const confirm = async () => {
    setBusy(true)
    try {
      await onConfirm()
      setOpen(false)
    } finally {
      setBusy(false)
    }
  }

  return (
    <span className="confirm-action" onKeyDown={handleKeyDown}>
      <PrimaryButton
        type="button"
        disabled={disabled || busy}
        style={style}
        className={className}
        aria-expanded={open}
        aria-controls={panelId}
        aria-haspopup="dialog"
        aria-busy={busy}
        onClick={() => setOpen((current) => !current)}
      >
        {children}
      </PrimaryButton>
      {open && (
        <span id={panelId} className="confirm-action-panel" role="dialog" aria-modal="false" aria-labelledby={messageId}>
          <span id={messageId} className="confirm-action-message">{message}</span>
          <PrimaryButton type="button" className="button-danger" disabled={busy} aria-busy={busy} onClick={() => void confirm()}>
            {busy ? 'Выполняем...' : 'Подтвердить'}
          </PrimaryButton>
          <PrimaryButton type="button" className="button-ghost" disabled={busy} onClick={close}>
            Отмена
          </PrimaryButton>
        </span>
      )}
    </span>
  )
}

export function SecretField({ label, configured, placeholder = 'Секрет хранится скрыто', value, onChange }: { label: string; configured?: boolean; placeholder?: string; value?: string; onChange?: (value: string) => void }) {
  const inputId = useId()
  const helpId = useId()
  const [visible, setVisible] = useState(false)

  return (
    <div className="form-field">
      <label htmlFor={inputId}>{label} <StatusBadge value={configured ? 'Configured' : 'Write-only'} /></label>
      <span className="password-field-row">
        <input
          id={inputId}
          value={value ?? ''}
          onChange={(e) => onChange?.(e.target.value)}
          placeholder={placeholder}
          type={visible ? 'text' : 'password'}
          autoComplete="new-password"
          aria-describedby={helpId}
        />
        <PrimaryButton
          type="button"
          className="button-ghost password-toggle"
          aria-label={visible ? `Скрыть ${label.toLowerCase()}` : `Показать ${label.toLowerCase()}`}
          aria-pressed={visible}
          title={visible ? 'Скрыть значение' : 'Показать значение'}
          onClick={() => setVisible((current) => !current)}
        >
          {visible ? 'Скрыть' : 'Показать'}
        </PrimaryButton>
      </span>
      <small id={helpId}>Значение не возвращается API и не показывается после сохранения.</small>
    </div>
  )
}

export function ValidationModeBadge({ label = 'Проверочный режим: внешние действия отключены' }: { label?: string }) {
  return <StatusBadge value={label} />
}

export function StateBlock({
  tone = 'neutral',
  title,
  description,
  action
}: {
  tone?: 'neutral' | 'success' | 'warning' | 'danger'
  title: string
  description?: string
  action?: React.ReactNode
}) {
  return (
    <div className={`state-block state-block-${tone}`}>
      <strong>{title}</strong>
      {description && <span>{description}</span>}
      {action && <div>{action}</div>}
    </div>
  )
}

export function FormValidationSummary({
  errors,
  title = 'Проверьте поля формы'
}: {
  errors: string[]
  title?: string
}) {
  if (errors.length === 0) return null

  return (
    <div className="form-validation-summary" role="alert" aria-live="polite">
      <strong>{title}</strong>
      <ul>
        {errors.map((error) => <li key={error}>{error}</li>)}
      </ul>
    </div>
  )
}

export function DataTableLite({
  columns,
  rows,
  emptyTitle = 'Нет данных',
  emptyDescription
}: {
  columns: string[]
  rows: Array<Array<React.ReactNode>>
  emptyTitle?: string
  emptyDescription?: string
}) {
  if (rows.length === 0) {
    return <StateBlock title={emptyTitle} description={emptyDescription} />
  }

  return (
    <div className="table-shell" role="region" aria-label="Таблица данных">
      <table className="data-table-lite">
        <thead>
          <tr>{columns.map((column) => <th key={column} scope="col">{column}</th>)}</tr>
        </thead>
        <tbody>
          {rows.map((row, rowIndex) => (
            <tr key={rowIndex}>
              {row.map((cell, cellIndex) => <td key={cellIndex}>{cell}</td>)}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
