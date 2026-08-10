import assert from 'node:assert/strict'
import { mkdtemp, mkdir, rm, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import test from 'node:test'
import { adminManualChunk } from '../apps/admin-panel/src/bundle-chunks'
import {
  ADMIN_BUNDLE_BUDGET,
  assertAdminBundleBudget,
  collectJavaScriptBundleStats
} from '../scripts/check-admin-bundle-budget.mjs'

test('admin chunk mapping isolates platform and stable React modules across path formats', () => {
  assert.equal(adminManualChunk('C:\\repo\\frontend\\packages\\api-client\\src\\index.ts'), 'platform-api')
  assert.equal(adminManualChunk('/repo/frontend/packages/ui/src/index.tsx'), 'platform-ui')
  assert.equal(adminManualChunk('/repo/frontend/node_modules/react-dom/client.js'), 'vendor-react')
  assert.equal(adminManualChunk('/repo/frontend/node_modules/react-router/dist/index.mjs'), 'vendor-react')
  assert.equal(adminManualChunk('/repo/frontend/apps/admin-panel/src/App.tsx'), undefined)
})

test('admin bundle budget reads JS assets and rejects each independent regression boundary', async () => {
  const root = await mkdtemp(join(tmpdir(), 'vpn-admin-bundle-'))
  const assets = join(root, 'assets')
  await mkdir(assets)
  try {
    await writeFile(join(assets, 'index.js'), 'const value = "budget";\n'.repeat(100))
    await writeFile(join(assets, 'ignored.css'), '.root{}')
    const stats = await collectJavaScriptBundleStats(root)
    assert.equal(stats.files.length, 1)
    assert.ok(stats.totalRawBytes > stats.totalGzipBytes)
    assert.doesNotThrow(() => assertAdminBundleBudget(stats))

    const passing = {
      files: Array.from({ length: ADMIN_BUNDLE_BUDGET.maxJavaScriptFiles }, (_, index) => ({ name: `${index}.js`, rawBytes: 1, gzipBytes: 1 })),
      maxChunkRawBytes: ADMIN_BUNDLE_BUDGET.maxChunkRawBytes,
      totalRawBytes: ADMIN_BUNDLE_BUDGET.maxTotalRawBytes,
      totalGzipBytes: ADMIN_BUNDLE_BUDGET.maxTotalGzipBytes
    }
    assert.doesNotThrow(() => assertAdminBundleBudget(passing))
    assert.throws(() => assertAdminBundleBudget({ ...passing, files: [...passing.files, { name: 'extra.js', rawBytes: 1, gzipBytes: 1 }] }), /JS files/)
    assert.throws(() => assertAdminBundleBudget({ ...passing, maxChunkRawBytes: ADMIN_BUNDLE_BUDGET.maxChunkRawBytes + 1 }), /max chunk/)
    assert.throws(() => assertAdminBundleBudget({ ...passing, totalRawBytes: ADMIN_BUNDLE_BUDGET.maxTotalRawBytes + 1 }), /total raw/)
    assert.throws(() => assertAdminBundleBudget({ ...passing, totalGzipBytes: ADMIN_BUNDLE_BUDGET.maxTotalGzipBytes + 1 }), /total gzip/)
  } finally {
    await rm(root, { recursive: true, force: true })
  }
})
