import test from 'node:test'
import assert from 'node:assert/strict'
import type { CreateVpnInboundPayload, CreateVpnPanelPayload, VpnInboundDto, VpnPanelDto } from '@vpn-platform/api-client'
import { isVpnInboundFormChanged, isVpnPanelFormChanged } from '../apps/admin-panel/src/admin-vpn-editors.ts'

const panel: VpnPanelDto = {
  id: 'panel-1', name: 'EU panel', baseUrl: 'https://panel.example.test', region: 'eu', status: 'Active', healthStatus: 'Healthy', login: 'admin', sslVerificationMode: 'Strict', apiVariant: 'X3UiOfficial', capacity: 100, usedCapacity: 1, autoCreateInbound: false, defaultInboundTemplateJson: '{}', lastHealthCheckAt: null, lastSyncAt: null, version: '2.4.9', lastError: '', revision: 4, createdAt: '2026-08-15T00:00:00Z', updatedAt: '2026-08-15T00:00:00Z'
}

const panelForm: CreateVpnPanelPayload = {
  name: ' EU panel ', baseUrl: 'https://panel.example.test/', login: ' admin ', password: '', region: ' eu ', capacity: 100, sslVerificationMode: 'strict', apiVariant: 'x3uiofficial', autoCreateInbound: false, defaultInboundTemplateJson: ' {} ', revision: 4
}

const inbound: VpnInboundDto = {
  id: 'inbound-1', vpnPanelId: 'panel-1', externalInboundId: '1', name: 'main-vless', protocol: 'vless', port: 443, listen: '', settingsJson: '{"clients":[]}', streamSettingsJson: '{"network":"tcp"}', sniffingJson: '{}', isDefault: true, isActive: true, capacity: 100, usedCapacity: 1, revision: 7
}

const inboundForm: CreateVpnInboundPayload = {
  name: 'main-vless', protocol: ' VLESS ', port: 443, listen: '', settingsJson: '{"clients":[]}', streamSettingsJson: '{"network":"tcp"}', sniffingJson: '{}', isDefault: true, isActive: true, capacity: 100, revision: 7
}

test('VPN panel editor matches backend normalization and treats a password as a change', () => {
  assert.equal(isVpnPanelFormChanged(panelForm, panel), false)
  assert.equal(isVpnPanelFormChanged({ ...panelForm, password: 'new-secret' }, panel), true)
  assert.equal(isVpnPanelFormChanged({ ...panelForm, capacity: 101 }, panel), true)
})

test('VPN inbound editor ignores revision and normalized protocol casing but detects mutable fields', () => {
  assert.equal(isVpnInboundFormChanged({ ...inboundForm, revision: 99 }, inbound), false)
  assert.equal(isVpnInboundFormChanged({ ...inboundForm, port: 8443 }, inbound), true)
  assert.equal(isVpnInboundFormChanged({ ...inboundForm, isDefault: false }, inbound), true)
  assert.equal(isVpnInboundFormChanged({ ...inboundForm, isActive: false, isDefault: true }, { ...inbound, isActive: false, isDefault: false }), false)
})
