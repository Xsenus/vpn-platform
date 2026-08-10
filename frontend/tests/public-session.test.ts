import test from 'node:test'
import assert from 'node:assert/strict'
import { ApiClientError } from '../packages/api-client/src/index.ts'
import {
  getPublicSessionCheckError,
  isPublicAccessTokenExpired,
  isPublicSessionRejected,
  publicSessionCheckFallback
} from '../apps/public-web/src/public-session.ts'

test('public session error guards distinguish refreshable and terminal responses', () => {
  const unauthorized = new ApiClientError('expired', 401, null)
  const forbidden = new ApiClientError('suspended', 403, null)
  const unavailable = new ApiClientError('unavailable', 503, null)

  assert.equal(isPublicAccessTokenExpired(unauthorized), true)
  assert.equal(isPublicAccessTokenExpired(forbidden), false)
  assert.equal(isPublicSessionRejected(unauthorized), true)
  assert.equal(isPublicSessionRejected(forbidden), true)
  assert.equal(isPublicSessionRejected(unavailable), false)
  assert.equal(isPublicSessionRejected(new Error('network')), false)
})

test('public session check keeps controlled error text for retry UI', () => {
  assert.equal(getPublicSessionCheckError(new Error('profile unavailable')), publicSessionCheckFallback)
  assert.equal(getPublicSessionCheckError(new Error('Сервер временно недоступен.')), 'Сервер временно недоступен.')
  assert.equal(getPublicSessionCheckError(null), publicSessionCheckFallback)
})
