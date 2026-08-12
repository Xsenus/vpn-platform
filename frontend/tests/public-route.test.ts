import assert from 'node:assert/strict'
import test from 'node:test'
import { getPublicRouteMetadata, publicRoutePaths } from '../apps/public-web/src/public-route'

test('public route metadata covers every page, trailing slashes and unknown paths', () => {
  assert.deepEqual(publicRoutePaths, ['/', '/tariffs', '/help', '/faq', '/account'])
  assert.equal(getPublicRouteMetadata('/').title, 'VPN Platform — быстрый VPN-доступ с автоматической выдачей')
  assert.equal(getPublicRouteMetadata('/tariffs').title, 'Тарифы — VPN Platform')
  assert.equal(getPublicRouteMetadata('/tariffs/').title, 'Тарифы — VPN Platform')
  assert.equal(getPublicRouteMetadata('/help').title, 'Помощь — VPN Platform')
  assert.equal(getPublicRouteMetadata('/faq').title, 'FAQ — VPN Platform')
  assert.equal(getPublicRouteMetadata('/account').title, 'Аккаунт — VPN Platform')
  assert.equal(getPublicRouteMetadata('/missing-page').title, 'Страница не найдена — VPN Platform')
  assert.match(getPublicRouteMetadata('/missing-page').description, /не найдена/i)
})
