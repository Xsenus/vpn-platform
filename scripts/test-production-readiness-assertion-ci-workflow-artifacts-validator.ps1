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

function Invoke-WorkflowArtifactsGuard {
    param([string]$CandidateWorkflowPath)

    $guardPath = Resolve-RepoPath "scripts/test-production-readiness-assertion-ci-workflow-artifacts.ps1"
    return & $guardPath -WorkflowPath $CandidateWorkflowPath -WriteJson 2>&1
}

function Assert-GuardFails {
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
        [void](Invoke-WorkflowArtifactsGuard -CandidateWorkflowPath $candidatePath)
    }
    catch {
        $failed = $true
        $message = $_.Exception.Message
    }

    if (-not $failed) {
        throw "Production readiness assertion CI workflow artifacts guard accepted tampered workflow: $Name"
    }

    if ($message.IndexOf($ExpectedMessage, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Production readiness assertion CI workflow artifacts guard failure '$Name' returned unexpected message: $message"
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
    throw "Production readiness assertion CI workflow was not found: $WorkflowPath"
}

$workflowFullPath = (Resolve-Path -LiteralPath $WorkflowPath).Path
$workflow = Get-Content -LiteralPath $workflowFullPath -Raw -Encoding UTF8
$tempDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("vpn-platform-readiness-assertion-ci-workflow-artifacts-validator-" + [System.Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDirectory -Force | Out-Null

try {
    $happyPathCopy = Join-Path $tempDirectory "happy-path.yml"
    Set-Content -LiteralPath $happyPathCopy -Value $workflow -Encoding UTF8
    [void](Invoke-WorkflowArtifactsGuard -CandidateWorkflowPath $happyPathCopy)

    $testedFailures = @()
    $testedFailures += Assert-GuardFails `
        -Name "missing-guard-step" `
        -WorkflowContent ($workflow.Replace("Guard production readiness assertion workflow artifacts", "Removed production readiness assertion workflow artifacts")) `
        -ExpectedMessage "workflow guard step"
    $testedFailures += Assert-GuardFails `
        -Name "missing-assertion-log-artifact" `
        -WorkflowContent ($workflow.Replace("tmp/production-readiness-assertion-ci-regression/production-readiness-assertion.log", "tmp/production-readiness-assertion-ci-regression/missing-assertion.log")) `
        -ExpectedMessage "required artifact"
    $testedFailures += Assert-GuardFails `
        -Name "bad-artifact-name" `
        -WorkflowContent ($workflow.Replace("name: production-readiness-assertion-ci-regression", "name: broken-readiness-assertion-artifact")) `
        -ExpectedMessage "artifact name"
    $testedFailures += Assert-GuardFails `
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
        Write-Host "production readiness assertion CI workflow artifacts guard validator passed $($result | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if (Test-Path -LiteralPath $tempDirectory) {
        Remove-Item -LiteralPath $tempDirectory -Recurse -Force
    }
}
