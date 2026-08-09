import { ApiClientError } from '@vpn-platform/api-client'

export const publicSessionEndedMessage = 'Сессия завершена или доступ к аккаунту отозван. Войдите заново.'
export const publicSessionCheckFallback = 'Не удалось проверить сессию. Повторите попытку, не выполняя новый вход.'

export function isPublicAccessTokenExpired(error: unknown) {
  return error instanceof ApiClientError && error.status === 401
}

export function isPublicSessionRejected(error: unknown) {
  return error instanceof ApiClientError && (error.status === 401 || error.status === 403)
}

export function getPublicSessionCheckError(error: unknown) {
  return error instanceof Error ? error.message : publicSessionCheckFallback
}
