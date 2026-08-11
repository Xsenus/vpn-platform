import type { AccessCredentialDto } from '@vpn-platform/api-client'

export type AdminAccessCommand = 'copy' | 'qr' | 'enable' | 'disable' | 'sync' | 'reset'

const expiredAccessReason = 'Срок VPN-доступа истёк. Ключ и provider-команды скрыты; доступно только отключение у провайдера и история.'

export function isAdminAccessExpired(access: AccessCredentialDto, now = new Date()) {
  if (access.subscriptionStatus === 'Expired') return true
  if (!access.expiryDate) return false

  const expiryTime = Date.parse(access.expiryDate)
  return !Number.isFinite(expiryTime) || expiryTime <= now.getTime()
}

export function getNextAdminAccessExpiryDelay(accesses: AccessCredentialDto[], now = new Date()) {
  const nowTime = now.getTime()
  const delays = accesses
    .filter((access) => access.status !== 'Revoked' && !access.isTerminal && access.expiryDate)
    .map((access) => Date.parse(access.expiryDate!))
    .filter((expiryTime) => Number.isFinite(expiryTime) && expiryTime > nowTime)
    .map((expiryTime) => expiryTime - nowTime)

  return delays.length > 0 ? Math.min(...delays) : null
}

export function getAdminAccessTerminalReason(access: AccessCredentialDto, now = new Date()) {
  if (access.status === 'Revoked') {
    return 'Доступ отозван. Ключ и provider-команды скрыты; доступна только история.'
  }

  if (access.subscriptionStatus === 'Cancelled') {
    return 'Родительская подписка отменена. Ключ и provider-команды скрыты; доступна только история.'
  }

  if (isAdminAccessExpired(access, now)) return expiredAccessReason

  if (access.isTerminal) {
    return 'VPN-доступ является терминальным. Ключ и provider-команды скрыты; доступна только история.'
  }

  return null
}

export function getAdminAccessCommandBlocker(access: AccessCredentialDto, command: AdminAccessCommand, now = new Date()) {
  if (command === 'disable'
    && isAdminAccessExpired(access, now)
    && access.subscriptionStatus !== 'Cancelled'
    && access.status !== 'Disabled'
    && access.status !== 'Revoked') {
    return null
  }

  const terminalReason = getAdminAccessTerminalReason(access, now)
  if (terminalReason) return terminalReason
  if ((command === 'copy' || command === 'qr') && !access.accessUri) {
    return 'VPN URI ещё не выдан.'
  }

  return null
}
