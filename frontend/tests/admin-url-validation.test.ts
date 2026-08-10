import assert from 'node:assert/strict'
import test from 'node:test'
import { isOptionalSafeAdminHttpUrl, validateTelegramBotUrlFields } from '../apps/admin-panel/src/admin-url-validation'

test('admin URL validation rejects embedded credentials and non-http schemes', () => {
  assert.equal(isOptionalSafeAdminHttpUrl(undefined), true)
  assert.equal(isOptionalSafeAdminHttpUrl(''), true)
  assert.equal(isOptionalSafeAdminHttpUrl(' https://api.example.test/path '), true)
  assert.equal(isOptionalSafeAdminHttpUrl('http://127.0.0.1:8080'), true)
  assert.equal(isOptionalSafeAdminHttpUrl('https://operator:secret@api.example.test/path'), false)
  assert.equal(isOptionalSafeAdminHttpUrl('javascript:alert(1)'), false)
  assert.equal(isOptionalSafeAdminHttpUrl('not a URL'), false)
})

test('Telegram settings report unsafe webhook and WebApp URLs before submit', () => {
  assert.deepEqual(validateTelegramBotUrlFields({
    webhookUrl: 'https://bot:secret@api.example.test/webhook',
    webAppUrl: 'https://user:secret@cabinet.example.test'
  }), [
    'Webhook URL должен быть корректным http/https адресом без логина и пароля.',
    'WebApp URL должен быть корректным http/https адресом без логина и пароля.'
  ])

  assert.deepEqual(validateTelegramBotUrlFields({
    webhookUrl: 'https://api.example.test/webhook',
    webAppUrl: 'https://cabinet.example.test'
  }), [])
})
