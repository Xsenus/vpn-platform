import type { AccessCredentialDto } from '@vpn-platform/api-client'

export type AdminAccessCommand = 'copy' | 'qr' | 'enable' | 'disable' | 'sync' | 'reset'

export function getAdminAccessTerminalReason(access: AccessCredentialDto) {
  if (access.status === 'Revoked') {
    return 'Доступ отозван. Ключ и provider-команды скрыты; доступна только история.'
  }

  if (access.subscriptionStatus === 'Cancelled') {
    return 'Родительская подписка отменена. Ключ и provider-команды скрыты; доступна только история.'
  }

  if (access.isTerminal) {
    return 'VPN-доступ является терминальным. Ключ и provider-команды скрыты; доступна только история.'
  }

  return null
}

export function getAdminAccessCommandBlocker(access: AccessCredentialDto, command: AdminAccessCommand) {
  const terminalReason = getAdminAccessTerminalReason(access)
  if (terminalReason) return terminalReason
  if ((command === 'copy' || command === 'qr') && !access.accessUri) {
    return 'VPN URI ещё не выдан.'
  }

  return null
}
