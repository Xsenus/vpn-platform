import test from 'node:test'
import assert from 'node:assert/strict'
import { existsSync, readFileSync, statSync } from 'node:fs'
import { dirname, join } from 'node:path'

function findRepositoryRoot(): string {
  let directory = process.cwd()
  while (directory !== dirname(directory)) {
    if (existsSync(join(directory, '.env.example')) && existsSync(join(directory, 'frontend'))) return directory
    directory = dirname(directory)
  }

  throw new Error('Repository root was not found for visual asset tests.')
}

const repositoryRoot = findRepositoryRoot()
const assetDirectory = join(repositoryRoot, 'frontend', 'packages', 'ui', 'src', 'assets')
const visualAssets = ['vpn-datacenter.webp', 'vpn-workspace.webp', 'vpn-global-network.webp']

test('visual backgrounds are bundled WebP assets instead of external runtime dependencies', () => {
  const sharedStyles = readFileSync(join(repositoryRoot, 'frontend', 'packages', 'ui', 'src', 'styles.css'), 'utf8')
  const publicStyles = readFileSync(join(repositoryRoot, 'frontend', 'apps', 'public-web', 'src', 'styles.css'), 'utf8')
  const adminStyles = readFileSync(join(repositoryRoot, 'frontend', 'apps', 'admin-panel', 'src', 'styles.css'), 'utf8')

  assert.doesNotMatch(`${sharedStyles}\n${publicStyles}\n${adminStyles}`, /url\(["']https?:\/\//i)
  assert.doesNotMatch(`${publicStyles}\n${adminStyles}`, /font-size:\s*clamp\(/i)

  for (const fileName of visualAssets) {
    const filePath = join(assetDirectory, fileName)
    assert.equal(existsSync(filePath), true, `${fileName} must exist`)
    assert.ok(statSync(filePath).size >= 50_000, `${fileName} must contain a production-quality image`)
    assert.equal(readFileSync(filePath).subarray(8, 12).toString('ascii'), 'WEBP', `${fileName} must be WebP`)
  }

  assert.match(publicStyles, /vpn-datacenter\.webp/)
  assert.match(publicStyles, /vpn-workspace\.webp/)
  assert.match(publicStyles, /vpn-global-network\.webp/)
  assert.match(adminStyles, /vpn-datacenter\.webp/)
})
