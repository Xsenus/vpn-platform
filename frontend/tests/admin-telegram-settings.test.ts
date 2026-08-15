import assert from 'node:assert/strict'
import test from 'node:test'
import { isTelegramBotSettingsFormChanged, telegramBotSettingsToForm } from '../apps/admin-panel/src/admin-telegram-settings'

const settings = {
  enabled: false,
  mode: 'LongPolling',
  publicBotUsername: 'vpnplatform_bot',
  hasBotToken: true,
  botTokenMasked: '1234***7890',
  webhookUrl: '',
  hasSecretToken: true,
  adminChatId: '',
  webAppUrl: 'https://cabinet.example.test',
  welcomeText: 'Добро пожаловать',
  instructionText: 'Инструкция',
  supportText: 'Поддержка',
  afterPaymentTextTemplate: 'Оплата получена',
  renewalTextTemplate: 'Продление',
  paymentFailedTextTemplate: 'Ошибка оплаты',
  subscriptionExpiredTextTemplate: 'Подписка истекла',
  revision: 3,
  generatedAt: '2033-03-04T05:06:07Z'
}

test('Telegram bot settings form detects normalized changes without exposing configured secrets', () => {
  const form = telegramBotSettingsToForm(settings)

  assert.equal(form.botToken, '')
  assert.equal(form.secretToken, '')
  assert.equal(isTelegramBotSettingsFormChanged(form, settings), false)
  assert.equal(isTelegramBotSettingsFormChanged({ ...form, publicBotUsername: '@vpnplatform_bot ' }, settings), false)
  assert.equal(isTelegramBotSettingsFormChanged({ ...form, welcomeText: 'Новое приветствие' }, settings), true)
  assert.equal(isTelegramBotSettingsFormChanged({ ...form, botToken: 'rotated-token' }, settings), true)
})
