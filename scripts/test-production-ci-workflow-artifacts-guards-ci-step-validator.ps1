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

function Invoke-CiStepGuard {
    param([string]$CandidateWorkflowPath)

    $guardPath = Resolve-RepoPath "scripts/test-production-ci-workflow-artifacts-guards-ci-step.ps1"
    return & $guardPath -WorkflowPath $CandidateWorkflowPath -WriteJson 2>&1
}

function Assert-CiStepGuardFails {
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
        [void](Invoke-CiStepGuard -CandidateWorkflowPath $candidatePath)
    }
    catch {
        $failed = $true
        $message = $_.Exception.Message
    }

    if (-not $failed) {
        throw "Production CI workflow artifacts aggregate CI step guard accepted tampered workflow: $Name"
    }

    if ($message.IndexOf($ExpectedMessage, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Production CI workflow artifacts aggregate CI step guard failure '$Name' returned unexpected message: $message"
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
$tempDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("vpn-platform-production-ci-workflow-artifacts-guards-ci-step-validator-" + [System.Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDirectory -Force | Out-Null

try {
    $happyPathCopy = Join-Path $tempDirectory "happy-path.yml"
    Set-Content -LiteralPath $happyPathCopy -Value $workflow -Encoding UTF8
    [void](Invoke-CiStepGuard -CandidateWorkflowPath $happyPathCopy)

    $testedFailures = @()
    $testedFailures += Assert-CiStepGuardFails `
        -Name "missing-ci-step-guard" `
        -WorkflowContent ($workflow.Replace("Guard production CI workflow artifacts guard steps", "Removed production CI workflow artifacts guard steps")) `
        -ExpectedMessage "aggregate CI step guard"
    $testedFailures += Assert-CiStepGuardFails `
        -Name "missing-ci-step-guard-command" `
        -WorkflowContent ($workflow.Replace("test-production-ci-workflow-artifacts-guards-ci-step.ps1 -WriteJson", "missing-production-ci-workflow-artifacts-guards-ci-step.ps1 -WriteJson")) `
        -ExpectedMessage "aggregate CI step guard command"
    $testedFailures += Assert-CiStepGuardFails `
        -Name "missing-ci-step-validator" `
        -WorkflowContent ($workflow.Replace("Guard production CI workflow artifacts guard steps regression", "Removed production CI workflow artifacts guard steps regression")) `
        -ExpectedMessage "aggregate CI step guard validator"
    $testedFailures += Assert-CiStepGuardFails `
        -Name "ci-step-guard-after-aggregate-guard" `
        -WorkflowContent ($workflow.
            Replace("test-production-ci-workflow-artifacts-guards-ci-step.ps1 -WriteJson", "__CI_STEP_GUARD_COMMAND__").
            Replace("test-production-ci-workflow-artifacts-guards.ps1 -WriteJson", "test-production-ci-workflow-artifacts-guards-ci-step.ps1 -WriteJson").
            Replace("__CI_STEP_GUARD_COMMAND__", "test-production-ci-workflow-artifacts-guards.ps1 -WriteJson")) `
        -ExpectedMessage "aggregate CI step guard before validator"

    $result = [ordered]@{
        status = "passed"
        workflowPath = $workflowFullPath
        testedFailures = @($testedFailures)
    }

    if ($WriteJson) {
        Write-Output ($result | ConvertTo-Json -Depth 8)
    }
    else {
        Write-Host "production CI workflow artifacts aggregate CI step guard validator passed $($result | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if (Test-Path -LiteralPath $tempDirectory) {
        Remove-Item -LiteralPath $tempDirectory -Recurse -Force
    }
}
