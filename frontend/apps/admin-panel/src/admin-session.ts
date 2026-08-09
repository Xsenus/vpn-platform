import { ApiClientError } from '@vpn-platform/api-client'

export const adminAccessDeniedMessage = 'У этой учетной записи нет доступа к админ-панели. Войдите с административной ролью.'
export const adminSessionEndedMessage = 'Сессия администратора завершена. Войдите заново.'

export function isAdminAccessTokenExpired(error: unknown) {
  return error instanceof ApiClientError && error.status === 401
}

export function isAdminSessionRejected(error: unknown) {
  return error instanceof ApiClientError && (error.status === 401 || error.status === 403)
}
