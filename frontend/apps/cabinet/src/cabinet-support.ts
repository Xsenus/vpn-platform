import type { CabinetSupportConversationDto } from '@vpn-platform/api-client'

export function validateSupportRequest(subject: string, text: string) {
  const errors: string[] = []
  if (subject.trim().length < 4) errors.push('Тема обращения должна быть не короче 4 символов.')
  if (subject.trim().length > 160) errors.push('Тема обращения должна быть короче 160 символов.')
  if (text.trim().length < 10) errors.push('Сообщение должно быть не короче 10 символов.')
  if (text.trim().length > 4000) errors.push('Сообщение должно быть короче 4000 символов.')
  return errors
}

export function validateSupportReply(text: string) {
  const errors: string[] = []
  if (text.trim().length < 2) errors.push('Ответ должен быть не короче 2 символов.')
  if (text.trim().length > 4000) errors.push('Ответ должен быть короче 4000 символов.')
  return errors
}

export function getSupportStatusMessage(status: string) {
  switch (status.toLowerCase()) {
    case 'open':
      return 'Обращение открыто. Команда поддержки увидит сообщение и ответит здесь.'
    case 'pending':
      return 'Поддержка ожидает уточнение или внешнюю проверку.'
    case 'closed':
      return 'Обращение закрыто. Можно переоткрыть его, если вопрос еще актуален.'
    default:
      return 'Статус обращения обновляется после ответа поддержки.'
  }
}

export function countOpenSupportConversations(conversations: CabinetSupportConversationDto[]) {
  return conversations.filter((item) => item.status === 'open' || item.status === 'pending').length
}

export function selectCurrentSupportConversation(conversations: CabinetSupportConversationDto[], selectedId: string) {
  return conversations.find((item) => item.id === selectedId) ?? conversations[0] ?? null
}

export function getSupportChannelLabel(channel: string) {
  return channel.toLowerCase() === 'telegram' ? 'Telegram' : 'Личный кабинет'
}
