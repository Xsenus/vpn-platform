export type ServerStateActionAvailability = {
  canEnterMaintenance: boolean
  canLeaveMaintenance: boolean
  canDisableAllocation: boolean
  canEnableAllocation: boolean
  canDisable: boolean
  canDelete: boolean
}

export function getServerStateActionAvailability(status: string, isAvailableForNewUsers: boolean, usedCapacity = 0): ServerStateActionAvailability {
  const canEnterMaintenance = ['Ready', 'Degraded', 'Full', 'Draining', 'Error'].includes(status)
  return {
    canEnterMaintenance,
    canLeaveMaintenance: status === 'Maintenance',
    canDisableAllocation: status === 'Ready' && isAvailableForNewUsers,
    canEnableAllocation: (status === 'Ready' || status === 'Draining') && !isAvailableForNewUsers,
    canDisable: status !== 'Disabled' && status !== 'Archived',
    canDelete: status !== 'Archived' && usedCapacity === 0
  }
}
