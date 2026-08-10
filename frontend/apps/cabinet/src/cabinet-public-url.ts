import { getSafeHttpUrl } from '@vpn-platform/ui'

type CabinetBrowserLocation = Pick<Location, 'protocol' | 'hostname' | 'port' | 'origin'>

const localPublicPortByCabinetPort: Record<string, string> = {
  '5174': '5173',
  '5474': '5473'
}

export function resolveCabinetPublicWebUrl(configuredUrl?: string, location?: CabinetBrowserLocation) {
  const safeConfiguredUrl = getSafeHttpUrl(configuredUrl)
  if (safeConfiguredUrl) {
    const parsed = new URL(safeConfiguredUrl)
    if (!parsed.search && !parsed.hash) return safeConfiguredUrl.replace(/\/+$/, '')
  }
  if (!location) return 'http://localhost:5173'

  const publicPort = localPublicPortByCabinetPort[location.port]
  return publicPort
    ? `${location.protocol}//${location.hostname}:${publicPort}`
    : location.origin
}
