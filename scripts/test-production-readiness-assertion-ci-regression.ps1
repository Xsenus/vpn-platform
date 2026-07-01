param(
    [string]$OutputDirectory = "",
    [switch]$Force,
    [switch]$WriteJson
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $repoRoot = Split-Path -Parent $PSScriptRoot
    return Join-Path $repoRoot $RelativePath
}

function Write-Utf8NoBomFile {
    param(
        [string]$PathValue,
        [string]$Content
    )

    [System.IO.File]::WriteAllText($PathValue, $Content, [System.Text.UTF8Encoding]::new($false))
}

function ConvertTo-CiMarkdown {
    param([object]$Result)

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("# Production readiness assertion CI regression")
    $lines.Add("")
    $lines.Add("- Status: ``$($Result.status)``")
    $lines.Add("- Assertion status: ``$($Result.assertion.status)``")
    $lines.Add("- Failed evidence reports: ``$($Result.assertion.failedEvidenceReportsCount)``")
    $lines.Add("- Blockers: ``$($Result.assertion.blockersCount)``")
    $lines.Add("- Result validator: ``$($Result.resultValidator.status)``")
    $lines.Add("- Result validator regression: ``$($Result.resultValidatorRegression.status)``")
    if ($null -ne $Result.ciSummaryValidatorRegression) {
        $lines.Add("- CI summary validator regression: ``$($Result.ciSummaryValidatorRegression.status)``")
    }
    if ($null -ne $Result.ciResultValidatorRegression) {
        $lines.Add("- CI result validator regression: ``$($Result.ciResultValidatorRegression.status)``")
    }
    if ($null -ne $Result.ciArtifactsValidatorRegression) {
        $lines.Add("- CI artifacts validator regression: ``$($Result.ciArtifactsValidatorRegression.status)``")
    }
    $lines.Add("")
    $lines.Add("## Artifacts")
    $lines.Add("- Assertion JSON: ``$($Result.assertion.resultJsonPath)``")
    $lines.Add("- Assertion Markdown: ``$($Result.assertion.resultMarkdownPath)``")
    $lines.Add("- Assertion log: ``$($Result.assertion.logPath)``")
    $lines.Add("- CI regression JSON: ``$($Result.resultJsonPath)``")
    $lines.Add("- CI regression Markdown: ``$($Result.resultMarkdownPath)``")

    return ($lines -join [Environment]::NewLine) + [Environment]::NewLine
}

function Add-GitHubStepSummary {
    param([string]$Markdown)

    $summaryPath = $env:GITHUB_STEP_SUMMARY
    if ([string]::IsNullOrWhiteSpace($summaryPath)) {
        return ""
    }

    $fullSummaryPath = [System.IO.Path]::GetFullPath($summaryPath)
    $summaryDirectory = Split-Path -Parent $fullSummaryPath
    if (-not [string]::IsNullOrWhiteSpace($summaryDirectory)) {
        New-Item -ItemType Directory -Path $summaryDirectory -Force | Out-Null
    }

    [System.IO.File]::AppendAllText(
        $fullSummaryPath,
        $Markdown + [Environment]::NewLine,
        [System.Text.UTF8Encoding]::new($false))

    return $fullSummaryPath
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$usingDefaultOutputDirectory = [string]::IsNullOrWhiteSpace($OutputDirectory)
if ($usingDefaultOutputDirectory) {
    $OutputDirectory = Join-Path (Resolve-RepoPath "tmp") "production-readiness-assertion-ci-regression-test"
}

$shouldCleanupGeneratedOutput = $usingDefaultOutputDirectory -and -not $WriteJson
$fullOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
if ((Test-Path -LiteralPath $fullOutputDirectory) -and -not $Force) {
    throw "Production readiness assertion CI regression output directory already exists. Pass -Force to overwrite: $fullOutputDirectory"
}

if (Test-Path -LiteralPath $fullOutputDirectory) {
    Remove-Item -LiteralPath $fullOutputDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $fullOutputDirectory -Force | Out-Null

$assertionMarkdownPath = Join-Path $fullOutputDirectory "production-readiness-assertion.md"
$assertionJsonPath = Join-Path $fullOutputDirectory "production-readiness-assertion.json"
$assertionLogPath = Join-Path $fullOutputDirectory "production-readiness-assertion.log"
$resultJsonPath = Join-Path $fullOutputDirectory "production-readiness-assertion-ci-regression-result.json"
$resultMarkdownPath = Join-Path $fullOutputDirectory "production-readiness-assertion-ci-regression-result.md"

$assertionError = ""
try {
    & (Resolve-RepoPath "scripts/assert-production-readiness.ps1") `
        -ReportPath (Resolve-RepoPath "docs/staging-smoke-report.template.json") `
        -OutputPath $assertionMarkdownPath `
        -Force *> $assertionLogPath
    $assertionExit = 0
}
catch {
    $assertionExit = 1
    $assertionError = $_.Exception.Message
    Add-Content -LiteralPath $assertionLogPath -Encoding UTF8 -Value $assertionError
}

if (-not (Test-Path -LiteralPath $assertionJsonPath -PathType Leaf)) {
    throw "Production readiness assertion CI regression expected assertion JSON artifact was not created: $assertionJsonPath"
}

$assertion = Get-Content -LiteralPath $assertionJsonPath -Raw -Encoding UTF8 | ConvertFrom-Json
$assertionStatus = [string]$assertion.status

if ($assertionStatus -eq "blocked" -and $assertionExit -eq 0) {
    throw "Production readiness assertion CI regression expected fail-closed exit for blocked assertion."
}

if ($assertionStatus -eq "production-ready" -and $assertionExit -ne 0) {
    throw "Production readiness assertion CI regression expected successful exit for production-ready assertion."
}

$validatorJson = & (Resolve-RepoPath "scripts/validate-production-readiness-assertion-result.ps1") `
    -ResultJsonPath $assertionJsonPath `
    -ResultMarkdownPath $assertionMarkdownPath `
    -WriteJson
$validator = $validatorJson | ConvertFrom-Json

if ([string]$validator.status -ne "valid") {
    throw "Production readiness assertion CI regression result validator did not pass."
}

if ($assertionStatus -eq "blocked") {
    $regressionJson = & (Resolve-RepoPath "scripts/test-production-readiness-assertion-result-validator.ps1") `
        -ResultJsonPath $assertionJsonPath `
        -ResultMarkdownPath $assertionMarkdownPath `
        -WriteJson
    $regression = $regressionJson | ConvertFrom-Json
}
else {
    $regression = [pscustomobject]@{
        status = "skipped"
        reason = "assertion is production-ready"
        testedFailures = @()
    }
}

if ($assertionStatus -eq "blocked" -and [string]$regression.status -ne "passed") {
    throw "Production readiness assertion CI regression result validator regression did not pass."
}

$result = [ordered]@{
    status = "passed"
    outputDirectory = $fullOutputDirectory
    assertion = [ordered]@{
        status = $assertionStatus
        exitCode = $assertionExit
        error = $assertionError
        resultJsonPath = $assertionJsonPath
        resultMarkdownPath = $assertionMarkdownPath
        logPath = $assertionLogPath
        failedEvidenceReportsCount = [int]$assertion.failedEvidenceReportsCount
        blockersCount = [int]$assertion.blockersCount
    }
    resultValidator = $validator
    resultValidatorRegression = $regression
    resultJsonPath = $resultJsonPath
    resultMarkdownPath = $resultMarkdownPath
}

$resultJson = $result | ConvertTo-Json -Depth 12
$resultMarkdown = ConvertTo-CiMarkdown -Result ([pscustomobject]$result)
Write-Utf8NoBomFile -PathValue $resultJsonPath -Content $resultJson
Write-Utf8NoBomFile -PathValue $resultMarkdownPath -Content $resultMarkdown

& (Resolve-RepoPath "scripts/validate-production-readiness-assertion-ci-regression-result.ps1") `
    -ResultJsonPath $resultJsonPath `
    -ResultMarkdownPath $resultMarkdownPath `
    -WriteJson | Out-Null

$ciResultValidatorRegressionJson = & (Resolve-RepoPath "scripts/test-production-readiness-assertion-ci-regression-result-validator.ps1") `
    -ResultJsonPath $resultJsonPath `
    -ResultMarkdownPath $resultMarkdownPath `
    -WriteJson
$ciResultValidatorRegression = $ciResultValidatorRegressionJson | ConvertFrom-Json

if ([string]$ciResultValidatorRegression.status -ne "passed") {
    throw "Production readiness assertion CI regression result validator regression did not pass."
}

$result["ciResultValidatorRegression"] = $ciResultValidatorRegression
$resultJson = $result | ConvertTo-Json -Depth 12
$resultMarkdown = ConvertTo-CiMarkdown -Result ([pscustomobject]$result)
Write-Utf8NoBomFile -PathValue $resultJsonPath -Content $resultJson
Write-Utf8NoBomFile -PathValue $resultMarkdownPath -Content $resultMarkdown

& (Resolve-RepoPath "scripts/validate-production-readiness-assertion-ci-regression-result.ps1") `
    -ResultJsonPath $resultJsonPath `
    -ResultMarkdownPath $resultMarkdownPath `
    -WriteJson | Out-Null

& (Resolve-RepoPath "scripts/validate-production-readiness-assertion-ci-summary.ps1") `
    -ResultJsonPath $resultJsonPath `
    -SummaryPath $resultMarkdownPath `
    -WriteJson | Out-Null

$artifactValidatorArgs = @{
    ArtifactDirectory = $fullOutputDirectory
    WriteJson = $true
}

& (Resolve-RepoPath "scripts/validate-production-readiness-assertion-ci-artifacts.ps1") @artifactValidatorArgs | Out-Null

$ciArtifactsValidatorRegressionJson = & (Resolve-RepoPath "scripts/test-production-readiness-assertion-ci-artifacts-validator.ps1") `
    -ArtifactDirectory $fullOutputDirectory `
    -WriteJson
$ciArtifactsValidatorRegression = $ciArtifactsValidatorRegressionJson | ConvertFrom-Json

if ([string]$ciArtifactsValidatorRegression.status -ne "passed") {
    throw "Production readiness assertion CI artifacts validator regression did not pass."
}

$result["ciArtifactsValidatorRegression"] = $ciArtifactsValidatorRegression
$resultJson = $result | ConvertTo-Json -Depth 12
$resultMarkdown = ConvertTo-CiMarkdown -Result ([pscustomobject]$result)
Write-Utf8NoBomFile -PathValue $resultJsonPath -Content $resultJson
Write-Utf8NoBomFile -PathValue $resultMarkdownPath -Content $resultMarkdown

& (Resolve-RepoPath "scripts/validate-production-readiness-assertion-ci-regression-result.ps1") `
    -ResultJsonPath $resultJsonPath `
    -ResultMarkdownPath $resultMarkdownPath `
    -WriteJson | Out-Null

& (Resolve-RepoPath "scripts/validate-production-readiness-assertion-ci-summary.ps1") `
    -ResultJsonPath $resultJsonPath `
    -SummaryPath $resultMarkdownPath `
    -WriteJson | Out-Null

$ciSummaryValidatorRegressionJson = & (Resolve-RepoPath "scripts/test-production-readiness-assertion-ci-summary-validator.ps1") `
    -ResultJsonPath $resultJsonPath `
    -SummaryPath $resultMarkdownPath `
    -WriteJson
$ciSummaryValidatorRegression = $ciSummaryValidatorRegressionJson | ConvertFrom-Json

if ([string]$ciSummaryValidatorRegression.status -ne "passed") {
    throw "Production readiness assertion CI summary validator regression did not pass."
}

$result["ciSummaryValidatorRegression"] = $ciSummaryValidatorRegression
$resultJson = $result | ConvertTo-Json -Depth 12
$resultMarkdown = ConvertTo-CiMarkdown -Result ([pscustomobject]$result)
Write-Utf8NoBomFile -PathValue $resultJsonPath -Content $resultJson
Write-Utf8NoBomFile -PathValue $resultMarkdownPath -Content $resultMarkdown

& (Resolve-RepoPath "scripts/validate-production-readiness-assertion-ci-regression-result.ps1") `
    -ResultJsonPath $resultJsonPath `
    -ResultMarkdownPath $resultMarkdownPath `
    -WriteJson | Out-Null

& (Resolve-RepoPath "scripts/validate-production-readiness-assertion-ci-summary.ps1") `
    -ResultJsonPath $resultJsonPath `
    -SummaryPath $resultMarkdownPath `
    -WriteJson | Out-Null

$githubStepSummaryPath = Add-GitHubStepSummary -Markdown $resultMarkdown

if (-not [string]::IsNullOrWhiteSpace($githubStepSummaryPath)) {
    & (Resolve-RepoPath "scripts/validate-production-readiness-assertion-ci-summary.ps1") `
        -ResultJsonPath $resultJsonPath `
        -SummaryPath $githubStepSummaryPath `
        -WriteJson | Out-Null
    $artifactValidatorArgs.StepSummaryPath = $githubStepSummaryPath
}

& (Resolve-RepoPath "scripts/validate-production-readiness-assertion-ci-artifacts.ps1") @artifactValidatorArgs | Out-Null

if ($shouldCleanupGeneratedOutput -and (Test-Path -LiteralPath $fullOutputDirectory)) {
    Remove-Item -LiteralPath $fullOutputDirectory -Recurse -Force
    $tmpDirectory = Join-Path $repoRoot "tmp"
    if ((Test-Path -LiteralPath $tmpDirectory) -and -not (Get-ChildItem -LiteralPath $tmpDirectory -Force)) {
        Remove-Item -LiteralPath $tmpDirectory -Force
    }
}

if ($WriteJson) {
    Write-Output $resultJson
}
else {
    Write-Host "production readiness assertion CI regression passed $($result | ConvertTo-Json -Depth 12 -Compress)"
}
