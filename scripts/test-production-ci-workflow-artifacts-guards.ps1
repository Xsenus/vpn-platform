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

function Invoke-Guard {
    param(
        [string]$Name,
        [string]$ScriptPath,
        [string]$WorkflowFullPath
    )

    $startedAt = [System.DateTimeOffset]::UtcNow
    [void](& $ScriptPath -WorkflowPath $WorkflowFullPath -WriteJson)

    return [ordered]@{
        name = $Name
        status = "passed"
        script = $ScriptPath
        durationMs = [int]([System.DateTimeOffset]::UtcNow - $startedAt).TotalMilliseconds
    }
}

if ([string]::IsNullOrWhiteSpace($WorkflowPath)) {
    $WorkflowPath = Resolve-RepoPath ".github/workflows/ci.yml"
}

if ([string]::IsNullOrWhiteSpace($WorkflowPath) -or -not (Test-Path -LiteralPath $WorkflowPath -PathType Leaf)) {
    throw "Production CI workflow was not found: $WorkflowPath"
}

$workflowFullPath = (Resolve-Path -LiteralPath $WorkflowPath).Path
$guards = @(
    [ordered]@{
        name = "production-readiness-assertion-workflow-artifacts"
        script = Resolve-RepoPath "scripts/test-production-readiness-assertion-ci-workflow-artifacts.ps1"
    },
    [ordered]@{
        name = "production-readiness-assertion-workflow-artifacts-validator"
        script = Resolve-RepoPath "scripts/test-production-readiness-assertion-ci-workflow-artifacts-validator.ps1"
    },
    [ordered]@{
        name = "production-evidence-workflow-artifacts"
        script = Resolve-RepoPath "scripts/test-production-evidence-handoff-package-archive-ci-workflow-artifacts.ps1"
    },
    [ordered]@{
        name = "production-evidence-workflow-artifacts-validator"
        script = Resolve-RepoPath "scripts/test-production-evidence-handoff-package-archive-ci-workflow-artifacts-validator.ps1"
    }
)

$results = @()

foreach ($guard in $guards) {
    if (-not (Test-Path -LiteralPath $guard.script -PathType Leaf)) {
        throw "Production CI workflow artifacts guard script was not found: $($guard.script)"
    }

    $results += Invoke-Guard -Name $guard.name -ScriptPath $guard.script -WorkflowFullPath $workflowFullPath
}

$result = [ordered]@{
    status = "passed"
    workflowPath = $workflowFullPath
    guardsCount = $results.Count
    guards = @($results)
}

if ($WriteJson) {
    Write-Output ($result | ConvertTo-Json -Depth 8)
}
else {
    Write-Host "production CI workflow artifacts guards passed $($result | ConvertTo-Json -Depth 8 -Compress)"
}
