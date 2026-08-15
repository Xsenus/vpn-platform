import type { CreateServerPayload, VpnNodeDto } from '@vpn-platform/api-client'

const protocolOrder = ['vless', 'vmess', 'trojan']
const systemTagKeys = new Set(['source', 'owner', 'ssh-auth', 'credentials', 'validation-mode', 'autodeploy-after-precheck'])

function text(value: unknown) {
  return String(value ?? '').trim()
}

function host(value: unknown) {
  return text(value).replace(/\/+$/, '')
}

function protocols(value: unknown) {
  const selected = new Set(text(value).toLowerCase().split(',').map((item) => item.trim()).filter(Boolean))
  return protocolOrder.filter((protocol) => selected.has(protocol)).join(',')
}

function tagValue(tagsCsv: string | null | undefined, key: string) {
  const normalizedKey = key.toLowerCase()
  for (const rawTag of (tagsCsv ?? '').split(',')) {
    const [rawKey, ...rawValue] = rawTag.split(':')
    if (rawKey?.trim().toLowerCase() === normalizedKey) return rawValue.join(':').trim()
  }
  return null
}

function tags(tagsCsv: string | null | undefined, owner: string, authMethod: string, credentials: string, validationMode: boolean) {
  const userTags = (tagsCsv ?? '')
    .split(',')
    .map((tag) => tag.trim())
    .filter(Boolean)
    .filter((tag) => !systemTagKeys.has(tag.split(':', 1)[0]?.trim().toLowerCase() ?? ''))
  return [
    ...userTags,
    'source:admin',
    `owner:${text(owner).toLowerCase() || 'admin'}`,
    `ssh-auth:${text(authMethod).toLowerCase() || 'ssh_key'}`,
    `credentials:${credentials}`,
    `validation-mode:${validationMode}`,
    'autodeploy-after-precheck:false'
  ].join(',')
}

export function isServerFormChanged(form: CreateServerPayload, server: VpnNodeDto) {
  const currentOwner = tagValue(server.tagsCsv, 'owner') ?? 'admin'
  const currentValidationMode = tagValue(server.tagsCsv, 'validation-mode') !== 'false'
  const authMethod = text(form.sshAuthMethod).toLowerCase() || server.sshAuthMethod || 'ssh_key'
  const rotatesSshCredential = text(form.sshCredential).length > 0
  const replacesSshCredentialWithLegacyPath = !rotatesSshCredential && text(form.sshPrivateKeyPath).length > 0
  const rotatesPanelPassword = text(form.panelPassword).length > 0
  const credentials = rotatesSshCredential || replacesSshCredentialWithLegacyPath || server.sshCredentialConfigured
    ? 'protected'
    : 'missing'
  const candidateTags = tags(
    form.tagsCsv,
    text(form.ownerType) || currentOwner,
    authMethod,
    credentials,
    form.validationMode ?? true
  )
  const currentTags = tags(server.tagsCsv, currentOwner, server.sshAuthMethod, server.sshCredentialConfigured ? 'protected' : 'missing', currentValidationMode)

  return rotatesSshCredential
    || replacesSshCredentialWithLegacyPath
    || rotatesPanelPassword
    || text(form.name) !== server.name
    || host(text(form.host) || form.ipAddress) !== server.host
    || text(form.ipAddress) !== server.ipAddress
    || (text(form.provider) || 'admin-vps') !== server.provider
    || text(form.region) !== server.region
    || text(form.country) !== server.country
    || text(form.datacenter) !== server.datacenter
    || form.capacity !== server.capacity
    || protocols(form.supportedProtocolsCsv || 'vless,vmess,trojan') !== server.supportedProtocolsCsv
    || form.priority !== server.priority
    || (text(form.sshUser) || 'root') !== server.sshUser
    || form.sshPort !== server.sshPort
    || form.skipHostKeyChecking !== server.skipHostKeyChecking
    || text(form.panelBaseUrl) !== server.panelBaseUrl
    || (text(form.panelUsername) || 'admin') !== server.panelUsername
    || (form.panelInboundId ?? null) !== server.panelInboundId
    || host(form.publicHostname) !== server.publicHostname
    || form.publicPort !== server.publicPort
    || (form.nodeGroupId ?? null) !== server.nodeGroupId
    || candidateTags !== currentTags
}
