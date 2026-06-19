param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactDirectory,

    [string]$StepSummaryPath = "",
    [switch]$RequireBlockedAssertion,
    [switch]$WriteJson
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $repoRoot = Split-Path -Parent $PSScriptRoot
    return Join-Path $repoRoot $RelativePath
}

function Assert-ExistingFile {
    param(
        [string]$PathValue,
        [string]$Label
    )

    if ([string]::IsNullOrWhiteSpace($PathValue) -or -not (Test-Path -LiteralPath $PathValue -PathType Leaf)) {
        throw "Production readiness assertion CI artifacts $Label was not found: $PathValue"
    }

    return (Resolve-Path -LiteralPath $PathValue).Path
}

function Assert-SamePath {
    param(
        [string]$ActualPath,
        [string]$ExpectedPath,
        [string]$Label
    )

    $actual = Assert-ExistingFile -PathValue $ActualPath -Label $Label
    $expected = Assert-ExistingFile -PathValue $ExpectedPath -Label $Label

    if ($actual -ne $expected) {
        throw "Production readiness assertion CI artifacts $Label path does not match expected artifact file."
    }
}

if ([string]::IsNullOrWhiteSpace($ArtifactDirectory) -or -not (Test-Path -LiteralPath $ArtifactDirectory -PathType Container)) {
    throw "Production readiness assertion CI artifacts directory was not found: $ArtifactDirectory"
}

$artifactDirectoryFullPath = (Resolve-Path -LiteralPath $ArtifactDirectory).Path

$requiredArtifacts = [ordered]@{
    resultJson = Join-Path $artifactDirectoryFullPath "production-readiness-assertion-ci-regression-result.json"
    resultMarkdown = Join-Path $artifactDirectoryFullPath "production-readiness-assertion-ci-regression-result.md"
    assertionJson = Join-Path $artifactDirectoryFullPath "production-readiness-assertion.json"
    assertionMarkdown = Join-Path $artifactDirectoryFullPath "production-readiness-assertion.md"
    assertionLog = Join-Path $artifactDirectoryFullPath "production-readiness-assertion.log"
}

foreach ($entry in $requiredArtifacts.GetEnumerator()) {
    Assert-ExistingFile -PathValue ([string]$entry.Value) -Label ([string]$entry.Key) | Out-Null
}

$resultValidatorArgs = @{
    ResultJsonPath = [string]$requiredArtifacts.resultJson
    ResultMarkdownPath = [string]$requiredArtifacts.resultMarkdown
    WriteJson = $true
}
if ($RequireBlockedAssertion) {
    $resultValidatorArgs.RequireBlockedAssertion = $true
}

$resultValidationJson = & (Resolve-RepoPath "scripts/validate-production-readiness-assertion-ci-regression-result.ps1") @resultValidatorArgs
$resultValidation = $resultValidationJson | ConvertFrom-Json
if ([string]$resultValidation.status -ne "valid") {
    throw "Production readiness assertion CI artifacts result validator did not pass."
}

$summaryValidationJson = & (Resolve-RepoPath "scripts/validate-production-readiness-assertion-ci-summary.ps1") `
    -ResultJsonPath ([string]$requiredArtifacts.resultJson) `
    -SummaryPath ([string]$requiredArtifacts.resultMarkdown) `
    -WriteJson
$summaryValidation = $summaryValidationJson | ConvertFrom-Json
if ([string]$summaryValidation.status -ne "valid") {
    throw "Production readiness assertion CI artifacts summary validator did not pass."
}

if (-not [string]::IsNullOrWhiteSpace($StepSummaryPath)) {
    $stepSummaryFullPath = Assert-ExistingFile -PathValue $StepSummaryPath -Label "step summary"
    $stepSummaryValidationJson = & (Resolve-RepoPath "scripts/validate-production-readiness-assertion-ci-summary.ps1") `
        -ResultJsonPath ([string]$requiredArtifacts.resultJson) `
        -SummaryPath $stepSummaryFullPath `
        -WriteJson
    $stepSummaryValidation = $stepSummaryValidationJson | ConvertFrom-Json
    if ([string]$stepSummaryValidation.status -ne "valid") {
        throw "Production readiness assertion CI artifacts step summary validator did not pass."
    }
}
else {
    $stepSummaryFullPath = ""
    $stepSummaryValidation = $null
}

$result = Get-Content -LiteralPath ([string]$requiredArtifacts.resultJson) -Raw -Encoding UTF8 | ConvertFrom-Json
if ((Resolve-Path -LiteralPath ([string]$result.outputDirectory)).Path -ne $artifactDirectoryFullPath) {
    throw "Production readiness assertion CI artifacts outputDirectory does not match artifact directory."
}

Assert-SamePath -ActualPath ([string]$result.resultJsonPath) -ExpectedPath ([string]$requiredArtifacts.resultJson) -Label "result JSON"
Assert-SamePath -ActualPath ([string]$result.resultMarkdownPath) -ExpectedPath ([string]$requiredArtifacts.resultMarkdown) -Label "result Markdown"
Assert-SamePath -ActualPath ([string]$result.assertion.resultJsonPath) -ExpectedPath ([string]$requiredArtifacts.assertionJson) -Label "assertion JSON"
Assert-SamePath -ActualPath ([string]$result.assertion.resultMarkdownPath) -ExpectedPath ([string]$requiredArtifacts.assertionMarkdown) -Label "assertion Markdown"
Assert-SamePath -ActualPath ([string]$result.assertion.logPath) -ExpectedPath ([string]$requiredArtifacts.assertionLog) -Label "assertion log"

$stepSummaryValidatorStatus = ""
if ($null -ne $stepSummaryValidation) {
    $stepSummaryValidatorStatus = [string]$stepSummaryValidation.status
}

$validation = [ordered]@{
    status = "valid"
    artifactDirectory = $artifactDirectoryFullPath
    requiredArtifactsCount = $requiredArtifacts.Count
    resultJsonPath = [string]$requiredArtifacts.resultJson
    resultMarkdownPath = [string]$requiredArtifacts.resultMarkdown
    assertionStatus = [string]$resultValidation.assertionStatus
    resultValidatorStatus = [string]$resultValidation.status
    summaryValidatorStatus = [string]$summaryValidation.status
    stepSummaryPath = $stepSummaryFullPath
    stepSummaryValidatorStatus = $stepSummaryValidatorStatus
}

if ($WriteJson) {
    Write-Output ($validation | ConvertTo-Json -Depth 6)
}
else {
    Write-Host "production readiness assertion CI artifacts valid $($validation | ConvertTo-Json -Depth 6 -Compress)"
}
