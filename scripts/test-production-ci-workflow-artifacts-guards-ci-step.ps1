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
        throw "Production CI workflow artifacts aggregate CI step is missing $Label`: $Expected"
    }
}

function Assert-Order {
    param(
        [string]$Content,
        [string]$Before,
        [string]$After,
        [string]$Label
    )

    $beforeIndex = $Content.IndexOf($Before, [System.StringComparison]::OrdinalIgnoreCase)
    $afterIndex = $Content.IndexOf($After, [System.StringComparison]::OrdinalIgnoreCase)

    if ($beforeIndex -lt 0 -or $afterIndex -lt 0 -or $beforeIndex -ge $afterIndex) {
        throw "Production CI workflow artifacts aggregate CI step order is invalid for $Label."
    }
}

if ([string]::IsNullOrWhiteSpace($WorkflowPath)) {
    $WorkflowPath = Resolve-RepoPath ".github/workflows/ci.yml"
}

if ([string]::IsNullOrWhiteSpace($WorkflowPath) -or -not (Test-Path -LiteralPath $WorkflowPath -PathType Leaf)) {
    throw "Production CI workflow was not found: $WorkflowPath"
}

$workflowFullPath = (Resolve-Path -LiteralPath $WorkflowPath).Path
$workflow = Get-Content -LiteralPath $workflowFullPath -Raw -Encoding UTF8

Assert-ContainsText -Content $workflow -Expected "backend:" -Label "backend job"
Assert-ContainsText -Content $workflow -Expected "Guard production CI workflow artifacts guard steps" -Label "aggregate CI step guard"
Assert-ContainsText -Content $workflow -Expected "test-production-ci-workflow-artifacts-guards-ci-step.ps1 -WriteJson" -Label "aggregate CI step guard command"
Assert-ContainsText -Content $workflow -Expected "Guard production CI workflow artifacts contracts" -Label "aggregate guard step"
Assert-ContainsText -Content $workflow -Expected "test-production-ci-workflow-artifacts-guards.ps1 -WriteJson" -Label "aggregate guard command"
Assert-ContainsText -Content $workflow -Expected "Guard production CI workflow artifacts contracts regression" -Label "aggregate validator step"
Assert-ContainsText -Content $workflow -Expected "test-production-ci-workflow-artifacts-guards-validator.ps1 -WriteJson" -Label "aggregate validator command"
Assert-ContainsText -Content $workflow -Expected "Setup .NET SDK from global.json" -Label "backend setup step"

Assert-Order -Content $workflow `
    -Before "Guard production CI workflow artifacts guard steps" `
    -After "test-production-ci-workflow-artifacts-guards-ci-step.ps1 -WriteJson" `
    -Label "aggregate CI step guard contains command"
Assert-Order -Content $workflow `
    -Before "test-production-ci-workflow-artifacts-guards-ci-step.ps1 -WriteJson" `
    -After "Guard production CI workflow artifacts contracts" `
    -Label "aggregate CI step guard before aggregate guard"
Assert-Order -Content $workflow `
    -Before "Guard production CI workflow artifacts contracts" `
    -After "test-production-ci-workflow-artifacts-guards.ps1 -WriteJson" `
    -Label "aggregate guard step contains command"
Assert-Order -Content $workflow `
    -Before "test-production-ci-workflow-artifacts-guards.ps1 -WriteJson" `
    -After "Guard production CI workflow artifacts contracts regression" `
    -Label "aggregate guard before aggregate validator"
Assert-Order -Content $workflow `
    -Before "Guard production CI workflow artifacts contracts regression" `
    -After "test-production-ci-workflow-artifacts-guards-validator.ps1 -WriteJson" `
    -Label "aggregate validator step contains command"
Assert-Order -Content $workflow `
    -Before "test-production-ci-workflow-artifacts-guards-validator.ps1 -WriteJson" `
    -After "Setup .NET SDK from global.json" `
    -Label "aggregate validator before backend setup"

$result = [ordered]@{
    status = "passed"
    workflowPath = $workflowFullPath
    aggregateCiStepGuardStep = "Guard production CI workflow artifacts guard steps"
    aggregateGuardStep = "Guard production CI workflow artifacts contracts"
    aggregateValidatorStep = "Guard production CI workflow artifacts contracts regression"
}

if ($WriteJson) {
    Write-Output ($result | ConvertTo-Json -Depth 8)
}
else {
    Write-Host "production CI workflow artifacts aggregate CI step guard passed $($result | ConvertTo-Json -Depth 8 -Compress)"
}
