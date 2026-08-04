const retryableProvisioningStatuses = new Set(['Failed', 'PrecheckFailed', 'Cancelled'])

const cancellableProvisioningStatuses = new Set([
  'Pending',
  'Requested',
  'AwaitingCredentials',
  'AwaitingConfirmation',
  'PrecheckQueued',
  'ReadyToDeploy',
  'DeployQueued',
  'Retrying'
])

export function canRetryProvisioningRun(status: string) {
  return retryableProvisioningStatuses.has(status)
}

export function canCancelProvisioningRun(status: string) {
  return cancellableProvisioningStatuses.has(status)
}
