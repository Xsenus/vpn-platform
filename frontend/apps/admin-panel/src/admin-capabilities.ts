import type { AdminSessionCapabilitiesDto } from '@vpn-platform/api-client'

export type AdminSectionId =
  | 'dashboard'
  | 'users'
  | 'support'
  | 'audit'
  | 'payments'
  | 'tariffs'
  | 'referrals'
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
  audit: 'adminWrite',
  payments: 'financeWrite',
  tariffs: 'adminWrite',
  referrals: 'adminWrite',
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

const adminSectionIds = new Set<AdminSectionId>(Object.keys(sectionWriteCapability) as AdminSectionId[])

export function parseAdminSectionHref(href?: string | null): AdminSectionId | null {
  if (!href || !/^#[a-z]+$/.test(href)) return null

  const section = href.slice(1) as AdminSectionId
  return adminSectionIds.has(section) ? section : null
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
