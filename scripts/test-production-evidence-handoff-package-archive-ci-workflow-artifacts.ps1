param(
    [string]$WorkflowPath = "",
    [switch]$WriteJson
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $repoRoot = Split-Path -Parent $PSScriptRoot
    return Join-Path $repoRoot $RelativePath
}

function Assert-ContainsText {
    param(
        [string]$Content,
        [string]$Expected,
        [string]$Label
    )

    if ($Content.IndexOf($Expected, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Production evidence CI workflow artifacts missing $Label`: $Expected"
    }
}

if ([string]::IsNullOrWhiteSpace($WorkflowPath)) {
    $WorkflowPath = Resolve-RepoPath ".github/workflows/ci.yml"
}

if ([string]::IsNullOrWhiteSpace($WorkflowPath) -or -not (Test-Path -LiteralPath $WorkflowPath -PathType Leaf)) {
    throw "Production evidence CI workflow was not found: $WorkflowPath"
}

$workflowFullPath = (Resolve-Path -LiteralPath $WorkflowPath).Path
$workflow = Get-Content -LiteralPath $workflowFullPath -Raw -Encoding UTF8

$requiredArtifacts = @(
    "tmp/production-evidence-handoff-package-archive-ci-regression/production-evidence-handoff-package-archive-ci-regression-result.json",
    "tmp/production-evidence-handoff-package-archive-ci-regression/production-evidence-handoff-package-archive-ci-regression-result.md"
)

Assert-ContainsText -Content $workflow -Expected "production-evidence:" -Label "job id"
Assert-ContainsText -Content $workflow -Expected "needs: backend" -Label "backend dependency"
Assert-ContainsText -Content $workflow -Expected "Guard production evidence workflow artifacts" -Label "workflow guard step"
Assert-ContainsText -Content $workflow -Expected "test-production-evidence-handoff-package-archive-ci-workflow-artifacts.ps1 -WriteJson" -Label "workflow guard command"
Assert-ContainsText -Content $workflow -Expected "Run production evidence handoff archive CI regression" -Label "run step"
Assert-ContainsText -Content $workflow -Expected "./scripts/test-production-evidence-handoff-package-archive-ci-regression.ps1" -Label "wrapper script"
Assert-ContainsText -Content $workflow -Expected "-OutputDirectory tmp/production-evidence-handoff-package-archive-ci-regression" -Label "output directory"
Assert-ContainsText -Content $workflow -Expected "Upload production evidence regression artifacts" -Label "upload step"
Assert-ContainsText -Content $workflow -Expected "actions/upload-artifact@v4" -Label "upload action"
Assert-ContainsText -Content $workflow -Expected "name: production-evidence-handoff-package-archive-ci-regression" -Label "artifact name"
Assert-ContainsText -Content $workflow -Expected "if-no-files-found: error" -Label "missing file policy"

foreach ($artifact in $requiredArtifacts) {
    Assert-ContainsText -Content $workflow -Expected $artifact -Label "required artifact"
}

$result = [ordered]@{
    status = "passed"
    workflowPath = $workflowFullPath
    artifactName = "production-evidence-handoff-package-archive-ci-regression"
    requiredArtifactsCount = $requiredArtifacts.Count
    requiredArtifacts = $requiredArtifacts
}

if ($WriteJson) {
    Write-Output ($result | ConvertTo-Json -Depth 6)
}
else {
    Write-Host "production evidence CI workflow artifacts passed $($result | ConvertTo-Json -Depth 6 -Compress)"
}
