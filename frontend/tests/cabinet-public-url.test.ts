import assert from 'node:assert/strict'
import test from 'node:test'
import { resolveCabinetPublicWebUrl } from '../apps/cabinet/src/cabinet-public-url'

const cabinetLocation = {
  protocol: 'https:',
  hostname: 'cabinet.example.test',
  port: '',
  origin: 'https://cabinet.example.test'
}

test('cabinet public URL accepts only credential-free http origins and trims trailing slashes', () => {
  assert.equal(resolveCabinetPublicWebUrl(' https://vpn.example.test/// ', cabinetLocation), 'https://vpn.example.test')
  assert.equal(resolveCabinetPublicWebUrl('http://127.0.0.1:5173/', cabinetLocation), 'http://127.0.0.1:5173')
  assert.equal(resolveCabinetPublicWebUrl('javascript:alert(1)', cabinetLocation), cabinetLocation.origin)
  assert.equal(resolveCabinetPublicWebUrl('data:text/html,unsafe', cabinetLocation), cabinetLocation.origin)
  assert.equal(resolveCabinetPublicWebUrl('https://user:secret@vpn.example.test', cabinetLocation), cabinetLocation.origin)
  assert.equal(resolveCabinetPublicWebUrl('https://vpn.example.test/?source=unsafe', cabinetLocation), cabinetLocation.origin)
  assert.equal(resolveCabinetPublicWebUrl('https://vpn.example.test/#unsafe', cabinetLocation), cabinetLocation.origin)
  assert.equal(resolveCabinetPublicWebUrl('not a URL', cabinetLocation), cabinetLocation.origin)
})

test('cabinet public URL preserves supported local public-web port mapping', () => {
  assert.equal(resolveCabinetPublicWebUrl(undefined, {
    protocol: 'http:',
    hostname: '127.0.0.1',
    port: '5174',
    origin: 'http://127.0.0.1:5174'
  }), 'http://127.0.0.1:5173')
  assert.equal(resolveCabinetPublicWebUrl('', {
    protocol: 'http:',
    hostname: 'localhost',
    port: '5474',
    origin: 'http://localhost:5474'
  }), 'http://localhost:5473')
})
