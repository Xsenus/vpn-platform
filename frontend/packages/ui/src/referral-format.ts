export function formatReferralRewardType(type: string) {
  const labels: Record<string, string> = {
    'bonus-days': 'Бонусные дни',
    cashback: 'Кэшбэк',
    discount: 'Скидка'
  }
  return labels[type.trim().toLowerCase()] ?? 'Реферальное начисление'
}

export function formatReferralRewardValue(value: number, currencyOrUnit: string) {
  const unit = currencyOrUnit.trim()
  const normalizedUnit = unit.toLowerCase()
  const formattedValue = new Intl.NumberFormat('ru-RU', { maximumFractionDigits: 2 }).format(value)

  if (normalizedUnit === 'day' || normalizedUnit === 'days') {
    if (!Number.isInteger(value)) return `${formattedValue} дн.`
    const absoluteValue = Math.abs(value)
    const lastTwoDigits = absoluteValue % 100
    const lastDigit = absoluteValue % 10
    const label = lastTwoDigits >= 11 && lastTwoDigits <= 14
      ? 'дней'
      : lastDigit === 1
        ? 'день'
        : lastDigit >= 2 && lastDigit <= 4
          ? 'дня'
          : 'дней'
    return `${formattedValue} ${label}`
  }

  if (/^[a-z]{3}$/i.test(unit)) {
    try {
      return new Intl.NumberFormat('ru-RU', {
        style: 'currency',
        currency: unit.toUpperCase(),
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
      }).format(value).replace(/\u00a0/g, ' ')
    } catch {
      // Unknown custom units fall through to a readable value and unit.
    }
  }

  return `${formattedValue} ${unit}`
}
