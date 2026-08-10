import assert from 'node:assert/strict'
import test from 'node:test'
import { getAdminPageMetadata } from '../apps/admin-panel/src/admin-page-metadata'

test('admin page metadata distinguishes hydration, login and active sections', () => {
  const section = {
    sectionLabel: 'Оплаты',
    sectionDescription: 'Платежные операции и возвраты.'
  }

  assert.deepEqual(getAdminPageMetadata({ ...section, hasAdminSession: false, sessionHydrating: true }), {
    title: 'Проверка сессии — Админ-панель VPN Platform',
    description: 'Проверяем сохраненную административную сессию VPN Platform.'
  })
  assert.equal(getAdminPageMetadata({ ...section, hasAdminSession: false, sessionHydrating: false }).title, 'Вход — Админ-панель VPN Platform')
  assert.deepEqual(getAdminPageMetadata({ ...section, hasAdminSession: true, sessionHydrating: false }), {
    title: 'Оплаты — Админ-панель VPN Platform',
    description: section.sectionDescription
  })
})
