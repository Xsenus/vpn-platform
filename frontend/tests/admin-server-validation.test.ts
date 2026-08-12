import assert from 'node:assert/strict'
import test from 'node:test'
import {
  isValidLegacySshKeyPath,
  isValidServerHost,
  isValidServerIpAddress,
  isValidSshUsername,
  validateServerForm
} from '../apps/admin-panel/src/admin-server-validation'

test('admin server target validation accepts supported host and SSH values', () => {
  assert.equal(isValidServerHost('vpn.example.test'), true)
  assert.equal(isValidServerHost('203.0.113.10'), true)
  assert.equal(isValidServerIpAddress('203.0.113.10'), true)
  assert.equal(isValidServerIpAddress('2001:db8::10'), true)
  assert.equal(isValidSshUsername('deploy_user@realm'), true)
  assert.equal(isValidLegacySshKeyPath('/run/secrets/vpn-platform/id_ed25519'), true)
})

test('admin server target validation rejects inventory and argument injection values', () => {
  assert.equal(isValidServerHost('vpn.example.test\ninjected'), false)
  assert.equal(isValidServerIpAddress('10.0.0.1\ninjected ansible_connection=local'), false)
  assert.equal(isValidSshUsername('root ansible_connection=local'), false)
  assert.equal(isValidLegacySshKeyPath('/run/secrets/operator key'), false)
  assert.equal(isValidLegacySshKeyPath('/run/secrets/key" --check'), false)
})

test('admin server form reports unsafe provisioning fields before submit', () => {
  const errors = validateServerForm({
    name: 'Unsafe node',
    host: 'vpn.example.test',
    ipAddress: '10.0.0.1\ninjected ansible_connection=local',
    sshUser: 'root ansible_connection=local',
    sshPrivateKeyPath: '/run/secrets/operator key',
    sshPort: 22,
    capacity: 100,
    priority: 1,
    panelBaseUrl: 'https://panel.example.test'
  })

  assert.deepEqual(errors, [
    'IP-адрес должен быть корректным IPv4 или IPv6 без пробелов.',
    'SSH-пользователь содержит недопустимые символы или пробелы.',
    'Путь к SSH-ключу должен быть абсолютным Unix-путём без пробелов и кавычек.'
  ])
})
