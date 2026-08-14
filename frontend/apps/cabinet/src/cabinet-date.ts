const dateTimeFormatter = new Intl.DateTimeFormat('ru-RU', {
  dateStyle: 'short',
  timeStyle: 'short'
})

const dateFormatter = new Intl.DateTimeFormat('ru-RU', {
  dateStyle: 'short'
})

function parseDate(value?: string | null) {
  if (!value) return null
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? null : date
}

export function formatCabinetDateTime(value?: string | null) {
  const date = parseDate(value)
  return date ? dateTimeFormatter.format(date) : '—'
}

export function formatCabinetDate(value?: string | null) {
  const date = parseDate(value)
  return date ? dateFormatter.format(date) : '—'
}
