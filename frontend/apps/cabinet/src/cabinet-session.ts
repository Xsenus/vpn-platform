import { ApiClientError } from '@vpn-platform/api-client'

export const cabinetSessionEndedMessage = 'Сессия завершена или доступ к аккаунту ограничен. Войдите заново.'

export function isCabinetSessionRejected(error: unknown) {
  return error instanceof ApiClientError && (error.status === 401 || error.status === 403)
}
