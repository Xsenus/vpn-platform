import test from 'node:test'
import assert from 'node:assert/strict'
import { ApiClientError } from '../packages/api-client/src/index.ts'
import { adminAccessDeniedMessage, adminSessionEndedMessage, isAdminAccessTokenExpired, isAdminSessionRejected } from '../apps/admin-panel/src/admin-session.ts'

test('admin session distinguishes refreshable access expiry from terminal rejection', () => {
  assert.equal(isAdminAccessTokenExpired(new ApiClientError('expired', 401, null)), true)
  assert.equal(isAdminAccessTokenExpired(new ApiClientError('forbidden', 403, null)), false)
  assert.equal(isAdminAccessTokenExpired(new Error('network')), false)
  assert.equal(isAdminSessionRejected(new ApiClientError('expired', 401, null)), true)
  assert.equal(isAdminSessionRejected(new ApiClientError('forbidden', 403, null)), true)
  assert.equal(isAdminSessionRejected(new ApiClientError('unavailable', 503, null)), false)
  assert.match(adminAccessDeniedMessage, /административной ролью/)
  assert.match(adminSessionEndedMessage, /Войдите заново/)
})
