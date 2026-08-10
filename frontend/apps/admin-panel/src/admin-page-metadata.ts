export type AdminPageMetadataInput = {
  sectionLabel: string
  sectionDescription: string
  hasAdminSession: boolean
  sessionHydrating: boolean
}

export type AdminPageMetadata = {
  title: string
  description: string
}

export function getAdminPageMetadata(input: AdminPageMetadataInput): AdminPageMetadata {
  if (input.sessionHydrating) {
    return {
      title: 'Проверка сессии — Админ-панель VPN Platform',
      description: 'Проверяем сохраненную административную сессию VPN Platform.'
    }
  }

  if (!input.hasAdminSession) {
    return {
      title: 'Вход — Админ-панель VPN Platform',
      description: 'Вход для администраторов и операторов VPN Platform.'
    }
  }

  return {
    title: `${input.sectionLabel} — Админ-панель VPN Platform`,
    description: input.sectionDescription
  }
}
