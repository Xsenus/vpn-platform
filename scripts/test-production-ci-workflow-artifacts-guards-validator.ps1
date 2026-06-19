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

function Invoke-AggregateGuard {
    param([string]$CandidateWorkflowPath)

    $guardPath = Resolve-RepoPath "scripts/test-production-ci-workflow-artifacts-guards.ps1"
    return & $guardPath -WorkflowPath $CandidateWorkflowPath -WriteJson 2>&1
}

function Assert-AggregateGuardFails {
    param(
        [string]$Name,
        [string]$WorkflowContent,
        [string]$ExpectedMessage
    )

    $candidatePath = Join-Path $tempDirectory "$Name.yml"
    Set-Content -LiteralPath $candidatePath -Value $WorkflowContent -Encoding UTF8

    $failed = $false
    $message = ""

    try {
        [void](Invoke-AggregateGuard -CandidateWorkflowPath $candidatePath)
    }
    catch {
        $failed = $true
        $message = $_.Exception.Message
    }

    if (-not $failed) {
        throw "Production CI workflow artifacts aggregate guard accepted tampered workflow: $Name"
    }

    if ($message.IndexOf($ExpectedMessage, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Production CI workflow artifacts aggregate guard failure '$Name' returned unexpected message: $message"
    }

    return [ordered]@{
        name = $Name
        message = $message
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
$tempDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("vpn-platform-production-ci-workflow-artifacts-guards-validator-" + [System.Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDirectory -Force | Out-Null

try {
    $happyPathCopy = Join-Path $tempDirectory "happy-path.yml"
    Set-Content -LiteralPath $happyPathCopy -Value $workflow -Encoding UTF8
    [void](Invoke-AggregateGuard -CandidateWorkflowPath $happyPathCopy)

    $testedFailures = @()
    $testedFailures += Assert-AggregateGuardFails `
        -Name "missing-aggregate-ci-step-guard-command" `
        -WorkflowContent ($workflow.Replace("test-production-ci-workflow-artifacts-guards-ci-step.ps1 -WriteJson", "missing-production-ci-workflow-artifacts-guards-ci-step.ps1 -WriteJson")) `
        -ExpectedMessage "aggregate CI step guard command"
    $testedFailures += Assert-AggregateGuardFails `
        -Name "missing-aggregate-ci-step-validator" `
        -WorkflowContent ($workflow.Replace("name: Guard production CI workflow artifacts guard steps regression", "name: Removed production CI workflow artifacts guard steps regression")) `
        -ExpectedMessage "aggregate CI step guard validator"
    $testedFailures += Assert-AggregateGuardFails `
        -Name "missing-readiness-guard-step" `
        -WorkflowContent ($workflow.Replace("Guard production readiness assertion workflow artifacts", "Removed production readiness assertion workflow artifacts")) `
        -ExpectedMessage "Production readiness assertion CI workflow artifacts missing workflow guard step"
    $testedFailures += Assert-AggregateGuardFails `
        -Name "missing-readiness-assertion-log-artifact" `
        -WorkflowContent ($workflow.Replace("tmp/production-readiness-assertion-ci-regression/production-readiness-assertion.log", "tmp/production-readiness-assertion-ci-regression/missing-assertion.log")) `
        -ExpectedMessage "Production readiness assertion CI workflow artifacts missing required artifact"
    $testedFailures += Assert-AggregateGuardFails `
        -Name "missing-production-evidence-result-artifact" `
        -WorkflowContent ($workflow.Replace("tmp/production-evidence-handoff-package-archive-ci-regression/production-evidence-handoff-package-archive-ci-regression-result.json", "tmp/production-evidence-handoff-package-archive-ci-regression/missing-result.json")) `
        -ExpectedMessage "Production evidence CI workflow artifacts missing required artifact"
    $testedFailures += Assert-AggregateGuardFails `
        -Name "missing-if-no-files-found-error" `
        -WorkflowContent ($workflow.Replace("if-no-files-found: error", "if-no-files-found: ignore")) `
        -ExpectedMessage "missing file policy"

    $result = [ordered]@{
        status = "passed"
        workflowPath = $workflowFullPath
        testedFailures = @($testedFailures)
    }

    if ($WriteJson) {
        Write-Output ($result | ConvertTo-Json -Depth 8)
    }
    else {
        Write-Host "production CI workflow artifacts aggregate guard validator passed $($result | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if (Test-Path -LiteralPath $tempDirectory) {
        Remove-Item -LiteralPath $tempDirectory -Recurse -Force
    }
}
