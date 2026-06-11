import React, { PropsWithChildren, useId, useState } from 'react'

export function PageShell({ title, children }: PropsWithChildren<{ title: string }>) {
  return (
    <main id="main-content" className="page-shell" tabIndex={-1}>
      <h1 className="page-title">{title}</h1>
      {children}
    </main>
  )
}

export function SkipLink({ href = '#main-content', label = 'Перейти к содержимому' }: { href?: string; label?: string }) {
  const activate = (event: React.MouseEvent<HTMLAnchorElement> | React.KeyboardEvent<HTMLAnchorElement>) => {
    if (!href.startsWith('#')) return

    const target = document.querySelector<HTMLElement>(href)
    if (!target) return

    event.preventDefault()
    target.focus({ preventScroll: true })
    target.scrollIntoView({ block: 'start' })
    window.history.replaceState(null, '', href)
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
  if (normalized.includes('active') || normalized.includes('ready') || normalized.includes('paid') || normalized.includes('healthy') || normalized.includes('enabled') || normalized.includes('completed') || normalized.includes('deployed') || normalized.includes('linked')) return 'success'
  if (normalized.includes('fail') || normalized.includes('error') || normalized.includes('blocked') || normalized.includes('cancel') || normalized.includes('disabled') || normalized.includes('unhealthy')) return 'danger'
  if (normalized.includes('pending') || normalized.includes('queued') || normalized.includes('grace') || normalized.includes('progress') || normalized.includes('sandbox') || normalized.includes('validation')) return 'warning'
  return 'neutral'
}

const statusLabels: Record<string, string> = {
  Active: 'Активно',
  Ready: 'Готово',
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
  PendingPayment: 'Ожидает оплаты',
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
  return <span className={`status-badge status-badge-${badgeTone(raw)}`}>{label || 'Неизвестно'}</span>
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

export function CopyButton({ value, label = 'Скопировать', disabled }: { value?: string | null; label?: string; disabled?: boolean }) {
  return (
    <PrimaryButton
      type="button"
      disabled={disabled || !value}
      onClick={() => value && void navigator.clipboard?.writeText(value)}
      title={value ? 'Скопировать в буфер обмена' : 'Нечего копировать'}
      className="button-secondary"
    >
      {label}
    </PrimaryButton>
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
      {help && <small>{help}</small>}
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
        aria-busy={busy}
        onClick={() => setOpen((current) => !current)}
      >
        {children}
      </PrimaryButton>
      {open && (
        <span id={panelId} className="confirm-action-panel" role="group" aria-labelledby={messageId}>
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
      <small>Значение не возвращается API и не показывается после сохранения.</small>
    </div>
  )
}

export function ValidationModeBadge({ label = 'Проверочный режим: внешние действия отключены' }: { label?: string }) {
  return <StatusBadge value={label} />
}

export function DataTableLite({ children }: PropsWithChildren) {
  return <div className="list-stack">{children}</div>
}
