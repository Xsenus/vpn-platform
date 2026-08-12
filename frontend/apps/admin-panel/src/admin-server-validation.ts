import { isOptionalSafeAdminHttpUrl } from './admin-url-validation'

export type ServerValidationFields = {
  name: string
  host: string
  ipAddress?: string | null
  sshUser?: string | null
  sshPrivateKeyPath?: string | null
  sshPort: number
  capacity: number
  priority: number
  panelBaseUrl?: string | null
  supportedProtocolsCsv?: string | null
  panelInboundId?: number | null
  publicHostname?: string | null
  publicPort?: number | null
}

function isValidIpv4(value: string) {
  const parts = value.split('.')
  return parts.length === 4 && parts.every((part) => /^\d{1,3}$/.test(part) && Number(part) <= 255)
}

function isValidIpv6(value: string) {
  if (!value.includes(':') || !/^[0-9a-f:.]+$/i.test(value)) return false
  try {
    return new URL(`http://[${value}]/`).hostname.length > 0
  } catch {
    return false
  }
}

export function isValidServerIpAddress(value?: string | null) {
  const normalized = String(value ?? '')
  return normalized.length > 0
    && normalized === normalized.trim()
    && (isValidIpv4(normalized) || isValidIpv6(normalized))
}

export function isValidServerHost(value?: string | null) {
  const normalized = String(value ?? '').trim().replace(/\/$/, '')
  if (!normalized || normalized.length > 253) return false
  if (isValidServerIpAddress(normalized)) return true
  if (/[\s/:]/.test(normalized)) return false
  return /^(?=.{1,253}$)([a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)*[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$/i.test(normalized)
}

export function isValidSshUsername(value?: string | null) {
  const normalized = String(value ?? '')
  return normalized.length > 0
    && normalized.length <= 64
    && /^[a-z0-9_][a-z0-9._@-]*$/i.test(normalized)
}

export function isValidLegacySshKeyPath(value?: string | null) {
  const normalized = String(value ?? '').trim()
  return normalized.length > 0
    && normalized.length <= 4096
    && normalized.startsWith('/')
    && !/[\s"\u0000-\u001f\u007f]/.test(normalized)
    && !normalized.startsWith('v1:')
    && !normalized.startsWith('validation-placeholder:')
    && !normalized.toUpperCase().includes('PRIVATE KEY')
}

export function validateServerForm(form: ServerValidationFields) {
  const errors: string[] = []
  const sshPort = Number(form.sshPort)
  const capacity = Number(form.capacity)
  const priority = Number(form.priority)
  const publicPort = Number(form.publicPort ?? 443)
  const panelInboundId = form.panelInboundId == null ? null : Number(form.panelInboundId)
  const protocols = String(form.supportedProtocolsCsv ?? '')
    .split(',')
    .map((protocol) => protocol.trim().toLowerCase())
    .filter(Boolean)
  if (!form.name.trim()) errors.push('Укажите название VPN-сервера.')
  if (!isValidServerHost(form.host)) errors.push('Укажите корректный Host / DNS VPN-сервера.')
  if (form.ipAddress && !isValidServerIpAddress(form.ipAddress)) errors.push('IP-адрес должен быть корректным IPv4 или IPv6 без пробелов.')
  if (form.sshUser && !isValidSshUsername(form.sshUser)) errors.push('SSH-пользователь содержит недопустимые символы или пробелы.')
  if (form.sshPrivateKeyPath && !isValidLegacySshKeyPath(form.sshPrivateKeyPath)) errors.push('Путь к SSH-ключу должен быть абсолютным Unix-путём без пробелов и кавычек.')
  if (!Number.isInteger(sshPort) || sshPort <= 0 || sshPort > 65535) errors.push('SSH-порт должен быть целым числом в диапазоне 1-65535.')
  if (!Number.isInteger(capacity) || capacity <= 0) errors.push('Емкость сервера должна быть целым числом больше 0.')
  if (!Number.isInteger(priority) || priority <= 0) errors.push('Приоритет должен быть целым числом больше 0.')
  if (!isOptionalSafeAdminHttpUrl(form.panelBaseUrl)) errors.push('URL панели должен быть корректным http/https адресом без логина и пароля.')
  if (protocols.length > 0 && protocols.some((protocol) => !['vless', 'vmess', 'trojan'].includes(protocol))) errors.push('Протоколы могут содержать только CSV-токены vless, vmess и trojan.')
  if (panelInboundId !== null && (!Number.isInteger(panelInboundId) || panelInboundId <= 0)) errors.push('Inbound ID должен быть целым числом больше 0.')
  if (form.publicHostname && !isValidServerHost(form.publicHostname)) errors.push('Публичный hostname должен быть корректным DNS-именем, IPv4 или IPv6.')
  if (!Number.isInteger(publicPort) || publicPort <= 0 || publicPort > 65535) errors.push('Публичный порт должен быть целым числом в диапазоне 1-65535.')
  return errors
}
