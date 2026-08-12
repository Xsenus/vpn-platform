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

export const adminSectionLabels: Record<AdminSectionId, string> = {
  dashboard: 'Дашборд',
  users: 'Пользователи',
  support: 'Поддержка',
  audit: 'Аудит',
  payments: 'Оплаты',
  tariffs: 'Тарифы',
  referrals: 'Рефералы',
  subscriptions: 'Подписки',
  vpn: 'VPN-доступы',
  nodes: 'Серверы',
  panels: '3x-ui панели',
  provisioning: 'Подготовка VPS',
  bot: 'Telegram-бот',
  releases: 'Что нового',
  faq: 'FAQ',
  content: 'Контент сайта',
  scenarios: 'Сценарии'
}

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

export const adminSectionIds = Object.freeze(Object.keys(sectionWriteCapability) as AdminSectionId[])
const adminSectionIdSet = new Set<AdminSectionId>(adminSectionIds)

export function parseAdminSectionHref(href?: string | null): AdminSectionId | null {
  if (!href || !/^#[a-z]+$/.test(href)) return null

  const section = href.slice(1) as AdminSectionId
  return adminSectionIdSet.has(section) ? section : null
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
