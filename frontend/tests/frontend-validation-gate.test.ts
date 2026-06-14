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

  throw new Error('Repository root was not found for frontend validation gate tests.')
}

function readRepoFile(...parts: string[]): string {
  return readFileSync(join(findRepositoryRoot(), ...parts), 'utf8')
}

test('frontend validation gates run install, typecheck, build, tests and audit', () => {
  const bashGate = readRepoFile('scripts', 'validate-frontend.sh')
  const powershellGate = readRepoFile('scripts', 'validate-frontend.ps1')

  for (const gate of [bashGate, powershellGate]) {
    assert.match(gate, /node --version/i)
    assert.match(gate, /npm --version/i)
    assert.match(gate, /npm ci/i)
    assert.match(gate, /npm run typecheck/i)
    assert.match(gate, /npm run build/i)
    assert.match(gate, /npm run test/i)
    assert.match(gate, /npm audit --audit-level=high/i)
  }

  assert.match(bashGate, /check-frontend-lockfile\.sh/i)
  assert.match(powershellGate, /Test-FrontendLockfileSafety/i)
  assert.match(powershellGate, /package-lock\.json/i)
})

test('frontend package and CI keep the same mandatory commands', () => {
  const packageJson = JSON.parse(readRepoFile('frontend', 'package.json')) as { scripts: Record<string, string> }
  const ciWorkflow = readRepoFile('.github', 'workflows', 'ci.yml')

  assert.match(packageJson.scripts.test, /tsx@4\.19\.2 --test/)
  assert.match(packageJson.scripts.typecheck, /apps\/public-web/)
  assert.match(packageJson.scripts.typecheck, /apps\/cabinet/)
  assert.match(packageJson.scripts.typecheck, /apps\/admin-panel/)
  assert.match(packageJson.scripts.build, /apps\/public-web/)
  assert.match(packageJson.scripts.build, /apps\/cabinet/)
  assert.match(packageJson.scripts.build, /apps\/admin-panel/)
  assert.match(packageJson.scripts.e2e, /playwright test/)
  assert.match(packageJson.scripts['e2e:public'], /playwright test --project=public-web/)
  assert.match(packageJson.scripts['e2e:cabinet'], /playwright test --project=cabinet/)
  assert.match(packageJson.scripts['e2e:admin'], /playwright test --project=admin-panel/)
  assert.match(packageJson.scripts['e2e:all-screens'], /playwright test --project=all-screens/)
  assert.match(packageJson.scripts['e2e:mobile'], /mobile-public/)
  assert.match(packageJson.scripts['e2e:mobile'], /mobile-cabinet/)
  assert.match(packageJson.scripts['e2e:mobile'], /mobile-admin/)
  assert.match(packageJson.scripts['e2e:console'], /public-web/)
  assert.match(packageJson.scripts['e2e:console'], /cabinet/)
  assert.match(packageJson.scripts['e2e:console'], /admin-panel/)
  assert.match(packageJson.scripts['e2e:console'], /all-screens/)
  assert.match(packageJson.scripts['e2e:console'], /mobile-public/)
  assert.match(packageJson.scripts['e2e:console'], /mobile-cabinet/)
  assert.match(packageJson.scripts['e2e:console'], /mobile-admin/)

  assert.match(ciWorkflow, /check-frontend-lockfile\.sh/)
  assert.match(ciWorkflow, /npm ci/)
  assert.match(ciWorkflow, /npm run typecheck/)
  assert.match(ciWorkflow, /npm run build/)
  assert.match(ciWorkflow, /npm run test/)
  assert.match(ciWorkflow, /playwright install --with-deps chromium/)
  assert.match(ciWorkflow, /npm run e2e:public/)
  assert.match(ciWorkflow, /npm run e2e:cabinet/)
  assert.match(ciWorkflow, /npm run e2e:admin/)
  assert.match(ciWorkflow, /npm run e2e:all-screens/)
  assert.match(ciWorkflow, /frontend\/playwright-report\/e2e/)
  assert.match(ciWorkflow, /npm audit --audit-level=high/)
})

test('frontend validation documentation and roadmap expose current green count', () => {
  const docs = readRepoFile('docs', 'frontend-validation-gate.md')
  const roadmap = readRepoFile('docs', 'PRODUCT_COMPLETION_ROADMAP.md')
  const testResults = readRepoFile('TEST_RESULTS.md')
  const releases = JSON.parse(readRepoFile('backend', 'src', 'VpnPlatform.Api', 'AppReleases', 'releases.json')) as Array<{ releaseId: string, version: string }>
  const frontendGateRelease = releases.find((release) => release.releaseId === '2026-06-13-frontend-validation-gate')

  assert.match(docs, /64\/64/)
  assert.match(roadmap, /\[x\] `P9-TST-002`/)
  assert.match(roadmap, /64\/64/)
  assert.match(testResults, /2026-06-13-frontend-validation-gate/)
  assert.equal(frontendGateRelease?.version, '0.89.0')
})
