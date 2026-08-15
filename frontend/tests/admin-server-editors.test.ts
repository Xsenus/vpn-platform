import assert from 'node:assert/strict'
import test from 'node:test'
import type { CreateServerPayload, VpnNodeDto } from '@vpn-platform/api-client'
import { isServerFormChanged } from '../apps/admin-panel/src/admin-server-editors'

const server = {
  name: 'EU Server',
  host: 'eu.example.test',
  ipAddress: '203.0.113.10',
  provider: 'admin-vps',
  region: 'eu',
  country: 'NL',
  datacenter: 'ams',
  capacity: 100,
  supportedProtocolsCsv: 'vless,vmess,trojan',
  priority: 10,
  tagsCsv: 'tier:premium,source:admin,owner:admin,ssh-auth:ssh_key,credentials:protected,validation-mode:true,autodeploy-after-precheck:false',
  sshUser: 'root',
  sshPort: 22,
  sshAuthMethod: 'ssh_key',
  sshCredentialConfigured: true,
  skipHostKeyChecking: true,
  panelBaseUrl: 'https://panel.example.test',
  panelUsername: 'admin',
  panelPasswordConfigured: true,
  panelInboundId: 1,
  publicHostname: 'vpn.example.test',
  publicPort: 443,
  nodeGroupId: null
} as VpnNodeDto

const form: CreateServerPayload = {
  name: server.name,
  host: server.host,
  ipAddress: server.ipAddress,
  provider: server.provider,
  region: server.region,
  country: server.country,
  datacenter: server.datacenter,
  capacity: server.capacity,
  supportedProtocolsCsv: 'Trojan, VLESS, vmess, vless',
  priority: server.priority,
  tagsCsv: server.tagsCsv,
  sshUser: server.sshUser,
  sshPort: server.sshPort,
  sshPrivateKeyPath: '',
  sshAuthMethod: server.sshAuthMethod,
  sshCredential: '',
  validationMode: true,
  ownerType: 'admin',
  skipHostKeyChecking: server.skipHostKeyChecking,
  panelBaseUrl: server.panelBaseUrl,
  panelUsername: server.panelUsername,
  panelPassword: '',
  panelInboundId: server.panelInboundId,
  publicHostname: server.publicHostname,
  publicPort: server.publicPort,
  nodeGroupId: null
}

test('server editor matches normalized backend fields without a false dirty state', () => {
  assert.equal(isServerFormChanged({ ...form, name: ` ${form.name} `, host: `${form.host}/` }, server), false)
  assert.equal(isServerFormChanged({ ...form, priority: 11 }, server), true)
})

test('server editor treats write-only credential and panel rotations as changes', () => {
  assert.equal(isServerFormChanged({ ...form, sshCredential: 'rotated' }, server), true)
  assert.equal(isServerFormChanged({ ...form, sshPrivateKeyPath: '/run/secrets/id_ed25519' }, server), true)
  assert.equal(isServerFormChanged({ ...form, panelPassword: 'rotated' }, server), true)
})
