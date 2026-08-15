import test from 'node:test'
import assert from 'node:assert/strict'
import { rebaseAdminRevision, rebaseAdminRevisionedDraft } from '../apps/admin-panel/src/admin-revision-recovery.ts'

test('admin revision recovery rebases only the revision used by the failed submission', () => {
  assert.equal(rebaseAdminRevision(3, 3, 4), 4)
  assert.equal(rebaseAdminRevision(5, 3, 4), 5)
  assert.equal(rebaseAdminRevision(null, 3, 4), null)
})

test('admin revision recovery preserves newer draft fields and state identity when already rebased', () => {
  const draft = { name: 'New local draft', revision: 3 }
  assert.deepEqual(rebaseAdminRevisionedDraft(draft, 3, 4), { name: 'New local draft', revision: 4 })

  const alreadyRebased = { ...draft, revision: 5 }
  assert.equal(rebaseAdminRevisionedDraft(alreadyRebased, 3, 4), alreadyRebased)
})
