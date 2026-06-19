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
        throw "Production readiness assertion CI workflow artifacts missing $Label`: $Expected"
    }
}

if ([string]::IsNullOrWhiteSpace($WorkflowPath)) {
    $WorkflowPath = Resolve-RepoPath ".github/workflows/ci.yml"
}

if ([string]::IsNullOrWhiteSpace($WorkflowPath) -or -not (Test-Path -LiteralPath $WorkflowPath -PathType Leaf)) {
    throw "Production readiness assertion CI workflow was not found: $WorkflowPath"
}

$workflowFullPath = (Resolve-Path -LiteralPath $WorkflowPath).Path
$workflow = Get-Content -LiteralPath $workflowFullPath -Raw -Encoding UTF8

$requiredArtifacts = @(
    "tmp/production-readiness-assertion-ci-regression/production-readiness-assertion-ci-regression-result.json",
    "tmp/production-readiness-assertion-ci-regression/production-readiness-assertion-ci-regression-result.md",
    "tmp/production-readiness-assertion-ci-regression/production-readiness-assertion.json",
    "tmp/production-readiness-assertion-ci-regression/production-readiness-assertion.md",
    "tmp/production-readiness-assertion-ci-regression/production-readiness-assertion.log"
)

Assert-ContainsText -Content $workflow -Expected "production-readiness-assertion:" -Label "job id"
Assert-ContainsText -Content $workflow -Expected "needs: backend" -Label "backend dependency"
Assert-ContainsText -Content $workflow -Expected "Run production readiness assertion CI regression" -Label "run step"
Assert-ContainsText -Content $workflow -Expected "./scripts/test-production-readiness-assertion-ci-regression.ps1" -Label "wrapper script"
Assert-ContainsText -Content $workflow -Expected "-OutputDirectory tmp/production-readiness-assertion-ci-regression" -Label "output directory"
Assert-ContainsText -Content $workflow -Expected "Upload production readiness assertion artifacts" -Label "upload step"
Assert-ContainsText -Content $workflow -Expected "actions/upload-artifact@v4" -Label "upload action"
Assert-ContainsText -Content $workflow -Expected "name: production-readiness-assertion-ci-regression" -Label "artifact name"
Assert-ContainsText -Content $workflow -Expected "if-no-files-found: error" -Label "missing file policy"

foreach ($artifact in $requiredArtifacts) {
    Assert-ContainsText -Content $workflow -Expected $artifact -Label "required artifact"
}

$result = [ordered]@{
    status = "passed"
    workflowPath = $workflowFullPath
    artifactName = "production-readiness-assertion-ci-regression"
    requiredArtifactsCount = $requiredArtifacts.Count
    requiredArtifacts = $requiredArtifacts
}

if ($WriteJson) {
    Write-Output ($result | ConvertTo-Json -Depth 6)
}
else {
    Write-Host "production readiness assertion CI workflow artifacts passed $($result | ConvertTo-Json -Depth 6 -Compress)"
}
