import test from 'node:test'
import assert from 'node:assert/strict'
import { existsSync, readFileSync } from 'node:fs'
import { dirname, join } from 'node:path'

function findRepositoryRoot(): string {
  let directory = process.cwd()
  while (directory !== dirname(directory)) {
    if (existsSync(join(directory, '.env.example')) && existsSync(join(directory, 'frontend'))) return directory
    directory = dirname(directory)
  }

  throw new Error('Repository root was not found for accessibility gate tests.')
}

test('all-screens keeps the automated WCAG A/AA desktop and mobile gate', () => {
  const root = findRepositoryRoot()
  const packageJson = JSON.parse(readFileSync(join(root, 'frontend', 'package.json'), 'utf8')) as {
    devDependencies?: Record<string, string>
  }
  const allScreens = readFileSync(join(root, 'frontend', 'e2e', 'all-screens.spec.ts'), 'utf8')

  assert.match(packageJson.devDependencies?.['@axe-core/playwright'] ?? '', /^\^4\./)
  assert.match(allScreens, /import AxeBuilder from '@axe-core\/playwright'/)
  assert.match(allScreens, /wcag2a.*wcag2aa.*wcag21a.*wcag21aa.*wcag22aa.*best-practice/s)
  assert.match(allScreens, /expectWcagQuality\(page, route\)/)
  assert.match(allScreens, /expectWcagQuality\(page, 'cabinet dashboard'\)/)
  assert.match(allScreens, /expectWcagQuality\(page, `admin \$\{section\}`\)/)
  assert.match(allScreens, /viewport\.name === 'compact-mobile'.*expectWcagQuality/s)
  assert.match(allScreens, /every admin section fits[\s\S]*test\.setTimeout\(600_000\)/)
  assert.doesNotMatch(allScreens, /disableRules|exclude\(/)
})

test('responsive matrix straddles every CSS breakpoint', () => {
  const root = findRepositoryRoot()
  const allScreens = readFileSync(join(root, 'frontend', 'e2e', 'all-screens.spec.ts'), 'utf8')
  const matrixSource = allScreens.slice(
    allScreens.indexOf('const responsiveViewports'),
    allScreens.indexOf('const captureVisualAudit')
  )
  const viewportWidths = new Set(Array.from(matrixSource.matchAll(/width:\s*(\d+)/g), (match) => Number(match[1])))
  const styleFiles = [
    ['frontend', 'packages', 'ui', 'src', 'styles.css'],
    ['frontend', 'apps', 'public-web', 'src', 'styles.css'],
    ['frontend', 'apps', 'cabinet', 'src', 'styles.css'],
    ['frontend', 'apps', 'admin-panel', 'src', 'styles.css']
  ]
  const breakpoints = new Set(styleFiles.flatMap((parts) => {
    const source = readFileSync(join(root, ...parts), 'utf8')
    return Array.from(source.matchAll(/@media\s*\(max-width:\s*(\d+)px\)/g), (match) => Number(match[1]))
  }))

  for (const breakpoint of breakpoints) {
    assert.ok(viewportWidths.has(breakpoint), `responsive matrix must include the ${breakpoint}px breakpoint`)
    assert.ok(viewportWidths.has(breakpoint + 1), `responsive matrix must include ${breakpoint + 1}px immediately above the breakpoint`)
  }
})
