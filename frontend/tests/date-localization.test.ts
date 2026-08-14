import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import test from 'node:test'

import { formatCabinetDate, formatCabinetDateTime } from '../apps/cabinet/src/cabinet-date.ts'

test('cabinet dates use explicit Russian formatting and safe fallbacks', () => {
  const localDate = new Date(2026, 5, 14, 15, 30).toISOString()

  assert.equal(formatCabinetDateTime(localDate), '14.06.2026, 15:30')
  assert.equal(formatCabinetDate(localDate), '14.06.2026')
  assert.equal(formatCabinetDateTime('not-a-date'), '—')
  assert.equal(formatCabinetDate(null), '—')
})

test('cabinet and admin do not depend on the browser default locale for visible dates', () => {
  const cabinetSource = readFileSync(new URL('../apps/cabinet/src/App.tsx', import.meta.url), 'utf8')
  const adminSource = readFileSync(new URL('../apps/admin-panel/src/App.tsx', import.meta.url), 'utf8')

  assert.doesNotMatch(cabinetSource, /\.toLocale(?:String|DateString)\(\)/)
  assert.match(adminSource, /date\.toLocaleString\('ru-RU'/)
  assert.doesNotMatch(adminSource, /date\.toLocaleString\(\)/)
})
