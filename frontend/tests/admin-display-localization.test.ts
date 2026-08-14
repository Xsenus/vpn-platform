import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import test from 'node:test'

test('admin user-facing metadata does not interpolate technical enum values directly', () => {
  const source = readFileSync(new URL('../apps/admin-panel/src/App.tsx', import.meta.url), 'utf8')

  for (const rawInterpolation of [
    /\{account\.mode\}/,
    /\{order\.channel \?\?/,
    /\{order\.type \?\?/,
    /\{conversation\.channel\}/,
    /\{message\.direction\}/,
    /\{item\.type\}:/
  ]) {
    assert.doesNotMatch(source, rawInterpolation)
  }

  assert.doesNotMatch(source, /статус \$\{(?:payment|refund|result|saved|check)\.status\}/)
})
