export function adminManualChunk(moduleId: string): string | undefined {
  const id = moduleId.replaceAll('\\', '/')

  if (id.includes('/packages/api-client/')) return 'platform-api'
  if (id.includes('/packages/ui/')) return 'platform-ui'
  if (
    id.includes('/node_modules/react/')
    || id.includes('/node_modules/react-dom/')
    || id.includes('/node_modules/react-router/')
    || id.includes('/node_modules/scheduler/')
  ) return 'vendor-react'

  return undefined
}
