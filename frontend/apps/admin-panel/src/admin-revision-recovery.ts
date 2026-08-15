export function rebaseAdminRevision(
  currentRevision: number | null | undefined,
  submittedRevision: number | null | undefined,
  latestRevision: number
): number | null | undefined {
  return currentRevision === submittedRevision ? latestRevision : currentRevision
}

export function rebaseAdminRevisionedDraft<T extends { revision?: number | null }>(
  draft: T,
  submittedRevision: number | null | undefined,
  latestRevision: number
): T {
  const revision = rebaseAdminRevision(draft.revision, submittedRevision, latestRevision)
  return revision === draft.revision ? draft : { ...draft, revision }
}
