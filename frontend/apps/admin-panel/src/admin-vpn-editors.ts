import type {
  CreateVpnInboundPayload,
  CreateVpnPanelPayload,
  VpnInboundDto,
  VpnPanelDto
} from '@vpn-platform/api-client'

export function isVpnPanelFormChanged(form: CreateVpnPanelPayload, current: VpnPanelDto): boolean {
  return Boolean(form.password?.trim())
    || (form.name.trim() || current.name) !== current.name
    || (form.baseUrl.trim().replace(/\/+$/, '') || current.baseUrl) !== current.baseUrl
    || (form.login.trim() || current.login) !== current.login
    || (form.region.trim() || current.region) !== current.region
    || form.capacity !== current.capacity
    || form.sslVerificationMode.toLowerCase() !== current.sslVerificationMode.toLowerCase()
    || form.apiVariant.toLowerCase() !== current.apiVariant.toLowerCase()
    || form.autoCreateInbound !== current.autoCreateInbound
    || (form.defaultInboundTemplateJson.trim() || current.defaultInboundTemplateJson) !== current.defaultInboundTemplateJson
}

export function isVpnInboundFormChanged(form: CreateVpnInboundPayload, current: VpnInboundDto): boolean {
  const effectiveIsDefault = form.isDefault && form.isActive
  return form.name !== current.name
    || form.protocol.trim().toLowerCase() !== current.protocol
    || form.port !== current.port
    || form.listen !== current.listen
    || form.settingsJson !== current.settingsJson
    || form.streamSettingsJson !== current.streamSettingsJson
    || form.sniffingJson !== current.sniffingJson
    || effectiveIsDefault !== current.isDefault
    || form.capacity !== current.capacity
    || form.isActive !== current.isActive
}

export function isVpnPanelReadOnly(panel: Pick<VpnPanelDto, 'status'> | undefined): boolean {
  return panel?.status === 'Archived'
}
