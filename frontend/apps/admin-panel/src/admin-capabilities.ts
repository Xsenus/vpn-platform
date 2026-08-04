import type { AdminSessionCapabilitiesDto } from '@vpn-platform/api-client'

export type AdminSectionId =
  | 'dashboard'
  | 'users'
  | 'support'
  | 'audit'
  | 'payments'
  | 'tariffs'
  | 'subscriptions'
  | 'vpn'
  | 'nodes'
  | 'panels'
  | 'provisioning'
  | 'bot'
  | 'releases'
  | 'faq'
  | 'content'
  | 'scenarios'

const sectionReadCapability: Partial<Record<AdminSectionId, keyof AdminSessionCapabilitiesDto>> = {
  support: 'supportRead',
  payments: 'financeRead',
  bot: 'botManage'
}

const sectionWriteCapability: Record<AdminSectionId, keyof AdminSessionCapabilitiesDto | null> = {
  dashboard: null,
  users: 'adminWrite',
  support: 'supportWrite',
  audit: null,
  payments: 'financeWrite',
  tariffs: 'adminWrite',
  subscriptions: 'adminWrite',
  vpn: 'vpnManage',
  nodes: 'provisioningManage',
  panels: 'vpnManage',
  provisioning: 'provisioningManage',
  bot: 'botManage',
  releases: 'adminWrite',
  faq: 'adminWrite',
  content: 'adminWrite',
  scenarios: 'adminWrite'
}

export function canAccessAdminSection(capabilities: AdminSessionCapabilitiesDto, section: AdminSectionId) {
  const required = sectionReadCapability[section] ?? 'adminRead'
  return capabilities[required]
}

export function canWriteAdminSection(capabilities: AdminSessionCapabilitiesDto, section: AdminSectionId) {
  const required = sectionWriteCapability[section]
  return required === null || capabilities[required]
}

export function filterAdminSectionIds(capabilities: AdminSessionCapabilitiesDto, sections: readonly AdminSectionId[]) {
  return sections.filter((section) => canAccessAdminSection(capabilities, section))
}
