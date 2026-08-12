export type PublicRouteMetadata = {
  title: string
  description: string
}

export const publicRoutePaths = ['/', '/tariffs', '/help', '/faq', '/account'] as const
type PublicRoutePath = typeof publicRoutePaths[number]
const publicRoutePathSet = new Set<string>(publicRoutePaths)

const metadataByPath: Record<PublicRoutePath, PublicRouteMetadata> = {
  '/': {
    title: 'VPN Platform — быстрый VPN-доступ с автоматической выдачей',
    description: 'Купите VPN-доступ онлайн: тарифы, оплата, личный кабинет, Telegram-бот и автоматическая выдача подключения.'
  },
  '/tariffs': {
    title: 'Тарифы — VPN Platform',
    description: 'Сравните доступные VPN-тарифы, выберите способ оплаты и начните оформление заказа.'
  },
  '/help': {
    title: 'Помощь — VPN Platform',
    description: 'Инструкции по покупке, получению VPN-доступа, подключению устройств и обращению в поддержку.'
  },
  '/faq': {
    title: 'FAQ — VPN Platform',
    description: 'Ответы на частые вопросы об оплате, выдаче доступа, подключении VPN и работе личного кабинета.'
  },
  '/account': {
    title: 'Аккаунт — VPN Platform',
    description: 'Вход и регистрация для оформления заказа и перехода в личный кабинет VPN Platform.'
  }
}

const notFoundMetadata: PublicRouteMetadata = {
  title: 'Страница не найдена — VPN Platform',
  description: 'Запрошенная страница не найдена. Вернитесь на главную или откройте раздел помощи VPN Platform.'
}

function normalizePublicPath(pathname: string) {
  if (!pathname || pathname === '/') return '/'
  return pathname.replace(/\/+$/, '') || '/'
}

export function getPublicRouteMetadata(pathname: string): PublicRouteMetadata {
  const normalizedPath = normalizePublicPath(pathname)
  return publicRoutePathSet.has(normalizedPath)
    ? metadataByPath[normalizedPath as PublicRoutePath]
    : notFoundMetadata
}
