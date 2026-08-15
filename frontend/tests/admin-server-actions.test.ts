import assert from 'node:assert/strict'
import test from 'node:test'
import { getServerStateActionAvailability } from '../apps/admin-panel/src/admin-server-actions'

test('server state actions expose only valid operational transitions', () => {
  assert.deepEqual(getServerStateActionAvailability('Ready', true), {
    canEnterMaintenance: true,
    canLeaveMaintenance: false,
    canDisableAllocation: true,
    canEnableAllocation: false,
    canDisable: true,
    canDelete: true
  })
  assert.deepEqual(getServerStateActionAvailability('Draining', false), {
    canEnterMaintenance: true,
    canLeaveMaintenance: false,
    canDisableAllocation: false,
    canEnableAllocation: true,
    canDisable: true,
    canDelete: true
  })
  assert.deepEqual(getServerStateActionAvailability('Maintenance', false), {
    canEnterMaintenance: false,
    canLeaveMaintenance: true,
    canDisableAllocation: false,
    canEnableAllocation: false,
    canDisable: true,
    canDelete: true
  })
})

test('disabled, archived and unprepared servers cannot be revived by allocation or maintenance controls', () => {
  for (const status of ['Disabled', 'Archived', 'New', 'Provisioning'] as const) {
    const availability = getServerStateActionAvailability(status, false)
    assert.equal(availability.canEnterMaintenance, false)
    assert.equal(availability.canLeaveMaintenance, false)
    assert.equal(availability.canDisableAllocation, false)
    assert.equal(availability.canEnableAllocation, false)
    assert.equal(availability.canDisable, status !== 'Disabled' && status !== 'Archived')
    assert.equal(availability.canDelete, status !== 'Archived')
  }
})

test('server with reserved capacity cannot expose delete action', () => {
  const availability = getServerStateActionAvailability('Ready', false, 1)

  assert.equal(availability.canDelete, false)
})
