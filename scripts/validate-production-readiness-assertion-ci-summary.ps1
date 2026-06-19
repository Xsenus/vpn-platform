param(
    [Parameter(Mandatory = $true)]
    [string]$ResultJsonPath,

    [Parameter(Mandatory = $true)]
    [string]$SummaryPath,

    [switch]$WriteJson
)

$ErrorActionPreference = "Stop"

function Assert-ExistingFile {
    param(
        [string]$PathValue,
        [string]$Label
    )

    if ([string]::IsNullOrWhiteSpace($PathValue) -or -not (Test-Path -LiteralPath $PathValue -PathType Leaf)) {
        throw "Production readiness assertion CI summary $Label was not found: $PathValue"
    }

    return (Resolve-Path -LiteralPath $PathValue).Path
}

function Assert-MarkdownContains {
    param(
        [string]$Markdown,
        [string]$Expected
    )

    if ($Markdown.IndexOf($Expected, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Production readiness assertion CI summary markdown is missing: $Expected"
    }
}

$resultJsonFullPath = Assert-ExistingFile -PathValue $ResultJsonPath -Label "JSON"
$summaryFullPath = Assert-ExistingFile -PathValue $SummaryPath -Label "Markdown"

$result = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
$summary = Get-Content -LiteralPath $summaryFullPath -Raw -Encoding UTF8

if ([string]$result.status -ne "passed") {
    throw "Production readiness assertion CI summary status must be passed."
}

$assertion = $result.assertion
if ($null -eq $assertion) {
    throw "Production readiness assertion CI summary assertion section is required."
}

$assertionStatus = [string]$assertion.status
if ($assertionStatus -notin @("blocked", "production-ready")) {
    throw "Production readiness assertion CI summary assertion status must be blocked or production-ready."
}

$resultValidator = $result.resultValidator
if ($null -eq $resultValidator -or [string]$resultValidator.status -ne "valid") {
    throw "Production readiness assertion CI summary result validator status must be valid."
}

$resultValidatorRegression = $result.resultValidatorRegression
if ($null -eq $resultValidatorRegression) {
    throw "Production readiness assertion CI summary result validator regression section is required."
}

if ($assertionStatus -eq "blocked" -and [string]$resultValidatorRegression.status -ne "passed") {
    throw "Production readiness assertion CI summary result validator regression status must be passed for blocked assertion."
}

if ($assertionStatus -eq "production-ready" -and [string]$resultValidatorRegression.status -ne "skipped") {
    throw "Production readiness assertion CI summary result validator regression status must be skipped for production-ready assertion."
}

foreach ($expected in @(
        "# Production readiness assertion CI regression",
        "- Status: ``passed``",
        "- Assertion status: ``$assertionStatus``",
        "- Failed evidence reports: ``$([int]$assertion.failedEvidenceReportsCount)``",
        "- Blockers: ``$([int]$assertion.blockersCount)``",
        "- Result validator: ``valid``",
        "- Result validator regression: ``$([string]$resultValidatorRegression.status)``",
        "## Artifacts",
        "Assertion JSON",
        "Assertion Markdown",
        "Assertion log",
        "CI regression JSON",
        "CI regression Markdown"
    )) {
    Assert-MarkdownContains -Markdown $summary -Expected $expected
}

foreach ($pathProperty in @(
        [string]$assertion.resultJsonPath,
        [string]$assertion.resultMarkdownPath,
        [string]$assertion.logPath,
        [string]$result.resultJsonPath,
        [string]$result.resultMarkdownPath
    )) {
    if ([string]::IsNullOrWhiteSpace($pathProperty)) {
        throw "Production readiness assertion CI summary artifact path is required."
    }

    Assert-MarkdownContains -Markdown $summary -Expected $pathProperty
}

$ciSummaryValidatorRegression = $result.ciSummaryValidatorRegression
if ($null -ne $ciSummaryValidatorRegression) {
    if ([string]$ciSummaryValidatorRegression.status -ne "passed") {
        throw "Production readiness assertion CI summary CI summary validator regression status must be passed."
    }

    Assert-MarkdownContains -Markdown $summary -Expected "- CI summary validator regression: ``passed``"
}

$ciResultValidatorRegression = $result.ciResultValidatorRegression
if ($null -ne $ciResultValidatorRegression) {
    if ([string]$ciResultValidatorRegression.status -ne "passed") {
        throw "Production readiness assertion CI summary CI result validator regression status must be passed."
    }

    Assert-MarkdownContains -Markdown $summary -Expected "- CI result validator regression: ``passed``"
}

$ciArtifactsValidatorRegression = $result.ciArtifactsValidatorRegression
if ($null -ne $ciArtifactsValidatorRegression) {
    if ([string]$ciArtifactsValidatorRegression.status -ne "passed") {
        throw "Production readiness assertion CI summary CI artifacts validator regression status must be passed."
    }

    Assert-MarkdownContains -Markdown $summary -Expected "- CI artifacts validator regression: ``passed``"
}

$ciSummaryValidatorRegressionStatus = ""
if ($null -ne $ciSummaryValidatorRegression) {
    $ciSummaryValidatorRegressionStatus = [string]$ciSummaryValidatorRegression.status
}

$ciResultValidatorRegressionStatus = ""
if ($null -ne $ciResultValidatorRegression) {
    $ciResultValidatorRegressionStatus = [string]$ciResultValidatorRegression.status
}

$ciArtifactsValidatorRegressionStatus = ""
if ($null -ne $ciArtifactsValidatorRegression) {
    $ciArtifactsValidatorRegressionStatus = [string]$ciArtifactsValidatorRegression.status
}

$validation = [ordered]@{
    status = "valid"
    resultJsonPath = $resultJsonFullPath
    summaryPath = $summaryFullPath
    assertionStatus = $assertionStatus
    failedEvidenceReportsCount = [int]$assertion.failedEvidenceReportsCount
    blockersCount = [int]$assertion.blockersCount
    resultValidatorStatus = [string]$resultValidator.status
    resultValidatorRegressionStatus = [string]$resultValidatorRegression.status
    ciSummaryValidatorRegressionStatus = $ciSummaryValidatorRegressionStatus
    ciResultValidatorRegressionStatus = $ciResultValidatorRegressionStatus
    ciArtifactsValidatorRegressionStatus = $ciArtifactsValidatorRegressionStatus
}

if ($WriteJson) {
    Write-Output ($validation | ConvertTo-Json -Depth 6)
}
else {
    Write-Host "production readiness assertion CI summary valid $($validation | ConvertTo-Json -Depth 6 -Compress)"
}
