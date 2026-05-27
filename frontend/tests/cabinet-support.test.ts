import test from 'node:test'
import assert from 'node:assert/strict'
import type { SupportConversationDto } from '../packages/api-client/src/index.ts'
import { countOpenSupportConversations, getSupportStatusMessage, selectCurrentSupportConversation, validateSupportReply, validateSupportRequest } from '../apps/cabinet/src/cabinet-support.ts'

function conversation(overrides: Partial<SupportConversationDto>): SupportConversationDto {
  return {
    id: 'support-1',
    channel: 'web',
    status: 'open',
    subject: 'Оплата',
    internalNote: '',
    createdAt: '2026-05-27T10:00:00Z',
    updatedAt: '2026-05-27T10:00:00Z',
    ...overrides
  }
}

test('cabinet support validates new conversation and reply text', () => {
  assert.deepEqual(validateSupportRequest('Оплата', 'Нужна помощь с платежом.'), [])
  assert.deepEqual(validateSupportReply('Спасибо'), [])

  assert.match(validateSupportRequest('О', 'коротко').join(' '), /Тема обращения/)
  assert.match(validateSupportRequest('Оплата', 'коротко').join(' '), /Сообщение/)
  assert.match(validateSupportReply('').join(' '), /Ответ/)
})

test('cabinet support explains statuses in Russian', () => {
  assert.match(getSupportStatusMessage('open'), /открыто/)
  assert.match(getSupportStatusMessage('pending'), /ожидает/)
  assert.match(getSupportStatusMessage('closed'), /закрыто/)
})

test('cabinet support selects current conversation and counts open queue', () => {
  const first = conversation({ id: 'support-1', status: 'closed' })
  const second = conversation({ id: 'support-2', status: 'pending' })
  const third = conversation({ id: 'support-3', status: 'open' })

  assert.equal(selectCurrentSupportConversation([first, second, third], 'support-2')?.id, 'support-2')
  assert.equal(selectCurrentSupportConversation([first], 'missing')?.id, 'support-1')
  assert.equal(countOpenSupportConversations([first, second, third]), 2)
})

