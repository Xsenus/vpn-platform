import test from 'node:test'
import assert from 'node:assert/strict'
import { ApiClientError } from '../packages/api-client/src/index.ts'
import { cabinetSessionEndedMessage, isCabinetSessionRejected } from '../apps/cabinet/src/cabinet-session.ts'

test('cabinet session rejects unauthorized and forbidden API responses only', () => {
  assert.equal(isCabinetSessionRejected(new ApiClientError('unauthorized', 401, null)), true)
  assert.equal(isCabinetSessionRejected(new ApiClientError('forbidden', 403, null)), true)
  assert.equal(isCabinetSessionRejected(new ApiClientError('bad request', 400, null)), false)
  assert.equal(isCabinetSessionRejected(new Error('network')), false)
  assert.match(cabinetSessionEndedMessage, /Войдите заново/)
})
