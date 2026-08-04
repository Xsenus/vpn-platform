import assert from 'node:assert/strict'
import test from 'node:test'
import { canCancelProvisioningRun, canRetryProvisioningRun } from '../apps/admin-panel/src/provisioning-state'

test('retry is available only for failed or cancelled provisioning runs', () => {
  for (const status of ['Failed', 'PrecheckFailed', 'Cancelled']) {
    assert.equal(canRetryProvisioningRun(status), true, status)
  }

  for (const status of ['Pending', 'Prechecking', 'Deploying', 'ReadyToDeploy', 'Succeeded', 'Deployed']) {
    assert.equal(canRetryProvisioningRun(status), false, status)
  }
})

test('cancel is unavailable once provisioning execution has started', () => {
  for (const status of ['Pending', 'PrecheckQueued', 'ReadyToDeploy', 'DeployQueued', 'Retrying']) {
    assert.equal(canCancelProvisioningRun(status), true, status)
  }

  for (const status of ['Running', 'Prechecking', 'Deploying', 'Failed', 'Succeeded', 'Cancelled']) {
    assert.equal(canCancelProvisioningRun(status), false, status)
  }
})
