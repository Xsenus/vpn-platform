import test from 'node:test'
import assert from 'node:assert/strict'
import { existsSync, readFileSync } from 'node:fs'
import { dirname, join } from 'node:path'

function findRepositoryRoot(): string {
  let directory = process.cwd()
  while (directory !== dirname(directory)) {
    if (existsSync(join(directory, '.env.example')) && existsSync(join(directory, 'frontend'))) {
      return directory
    }

    directory = dirname(directory)
  }

  throw new Error('Repository root was not found for user help tests.')
}

function readRepoFile(...parts: string[]): string {
  return readFileSync(join(findRepositoryRoot(), ...parts), 'utf8')
}

test('public and cabinet apps expose user help journey', () => {
  const publicApp = readRepoFile('frontend', 'apps', 'public-web', 'src', 'App.tsx')
  const cabinetApp = readRepoFile('frontend', 'apps', 'cabinet', 'src', 'App.tsx')
  const guide = readRepoFile('docs', 'user-guide.md')

  assert.match(publicApp, /path="\/help"/)
  assert.match(publicApp, /Как купить и подключить VPN/)
  assert.match(publicApp, /После оплаты вернитесь в кабинет/)
  assert.match(cabinetApp, /Как пользоваться сервисом/)
  assert.match(cabinetApp, /Скопируйте ссылку или откройте QR-код/)
  assert.match(cabinetApp, /href=\{`\$\{publicWebUrl\}\/help`\}>Помощь<\/a>/)
  assert.match(guide, /P10-DOC-003/)
  assert.match(guide, /Telegram Stars/)
})
