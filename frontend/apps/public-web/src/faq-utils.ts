import { FaqItem } from '@vpn-platform/api-client'

export const FAQ_ALL_CATEGORY = 'Все'
export const FAQ_DEFAULT_CATEGORY = 'Общее'

export function normalizeFaqCategory(category?: string | null) {
  const value = category?.trim()
  return value || FAQ_DEFAULT_CATEGORY
}

export function getFaqCategories(items: FaqItem[]) {
  const categories = new Set(items.map((item) => normalizeFaqCategory(item.category)))
  return [
    FAQ_ALL_CATEGORY,
    ...Array.from(categories).sort((left, right) => left.localeCompare(right, 'ru'))
  ]
}

export function filterFaqItems(items: FaqItem[], category: string, search: string) {
  const normalizedCategory = category || FAQ_ALL_CATEGORY
  const query = search.trim().toLowerCase()

  return items.filter((item) => {
    const itemCategory = normalizeFaqCategory(item.category)
    const matchesCategory = normalizedCategory === FAQ_ALL_CATEGORY || itemCategory === normalizedCategory
    const searchableText = [item.question, item.answer, itemCategory].join(' ').toLowerCase()
    return matchesCategory && (!query || searchableText.includes(query))
  })
}
