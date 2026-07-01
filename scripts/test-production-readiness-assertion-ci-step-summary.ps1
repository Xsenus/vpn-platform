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

function Assert-ExistingFile {
    param(
        [string]$PathValue,
        [string]$Label
    )

    if ([string]::IsNullOrWhiteSpace($PathValue) -or -not (Test-Path -LiteralPath $PathValue -PathType Leaf)) {
        throw "Production readiness assertion CI step summary $Label was not found: $PathValue"
    }

    return (Resolve-Path -LiteralPath $PathValue).Path
}

$usingDefaultOutputDirectory = [string]::IsNullOrWhiteSpace($OutputDirectory)
if ($usingDefaultOutputDirectory) {
    $OutputDirectory = Join-Path (Resolve-RepoPath "tmp") "production-readiness-assertion-ci-step-summary-test"
}

$fullOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
if ((Test-Path -LiteralPath $fullOutputDirectory) -and -not $Force) {
    throw "Production readiness assertion CI step summary output directory already exists. Pass -Force to overwrite: $fullOutputDirectory"
}

if (Test-Path -LiteralPath $fullOutputDirectory) {
    Remove-Item -LiteralPath $fullOutputDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $fullOutputDirectory -Force | Out-Null

$summaryPath = Join-Path $fullOutputDirectory "production-readiness-assertion-ci-step-summary.md"
$previousSummaryPath = $env:GITHUB_STEP_SUMMARY

try {
    $env:GITHUB_STEP_SUMMARY = $summaryPath

    $wrapperJson = & (Resolve-RepoPath "scripts/test-production-readiness-assertion-ci-regression.ps1") `
        -OutputDirectory $fullOutputDirectory `
        -Force `
        -WriteJson
    $wrapper = $wrapperJson | ConvertFrom-Json

    if ([string]$wrapper.status -ne "passed") {
        throw "Production readiness assertion CI step summary wrapper did not pass."
    }

    $resultJsonPath = Assert-ExistingFile -PathValue ([string]$wrapper.resultJsonPath) -Label "result JSON"
    $resultMarkdownPath = Assert-ExistingFile -PathValue ([string]$wrapper.resultMarkdownPath) -Label "result Markdown"
    $summaryFullPath = Assert-ExistingFile -PathValue $summaryPath -Label "Markdown"

    $summaryValidatorJson = & (Resolve-RepoPath "scripts/validate-production-readiness-assertion-ci-summary.ps1") `
        -ResultJsonPath $resultJsonPath `
        -SummaryPath $summaryFullPath `
        -WriteJson
    $summaryValidator = $summaryValidatorJson | ConvertFrom-Json

    if ([string]$summaryValidator.status -ne "valid") {
        throw "Production readiness assertion CI step summary validator did not pass."
    }

    $resultMarkdown = Get-Content -LiteralPath $resultMarkdownPath -Raw -Encoding UTF8
    $summaryMarkdown = Get-Content -LiteralPath $summaryFullPath -Raw -Encoding UTF8
    if ($summaryMarkdown.TrimEnd() -ne $resultMarkdown.TrimEnd()) {
        throw "Production readiness assertion CI step summary Markdown does not match result Markdown."
    }

    foreach ($expected in @(
            "# Production readiness assertion CI regression",
            "- Status: ``passed``",
            "- Assertion status: ``$([string]$wrapper.assertion.status)``",
            "- CI summary validator regression: ``passed``",
            "- CI result validator regression: ``passed``",
            "- CI artifacts validator regression: ``passed``",
            "## Artifacts",
            [string]$wrapper.resultJsonPath,
            [string]$wrapper.resultMarkdownPath
        )) {
        if ($summaryMarkdown.IndexOf($expected, [System.StringComparison]::Ordinal) -lt 0) {
            throw "Production readiness assertion CI step summary markdown is missing: $expected"
        }
    }

    $result = [ordered]@{
        status = "passed"
        outputDirectory = $fullOutputDirectory
        resultJsonPath = $resultJsonPath
        resultMarkdownPath = $resultMarkdownPath
        summaryPath = $summaryFullPath
        assertionStatus = [string]$wrapper.assertion.status
        summaryValidatorStatus = [string]$summaryValidator.status
        ciSummaryValidatorRegressionStatus = [string]$wrapper.ciSummaryValidatorRegression.status
        ciResultValidatorRegressionStatus = [string]$wrapper.ciResultValidatorRegression.status
        ciArtifactsValidatorRegressionStatus = [string]$wrapper.ciArtifactsValidatorRegression.status
    }

    if ($WriteJson) {
        Write-Output ($result | ConvertTo-Json -Depth 6)
    }
    else {
        Write-Host "production readiness assertion CI step summary passed $($result | ConvertTo-Json -Depth 6 -Compress)"
    }
}
finally {
    $env:GITHUB_STEP_SUMMARY = $previousSummaryPath

    if ($usingDefaultOutputDirectory -and -not $WriteJson) {
        if (Test-Path -LiteralPath $fullOutputDirectory) {
            Remove-Item -LiteralPath $fullOutputDirectory -Recurse -Force
        }

        $tmpDirectory = Resolve-RepoPath "tmp"
        if ((Test-Path -LiteralPath $tmpDirectory) -and -not (Get-ChildItem -LiteralPath $tmpDirectory -Force)) {
            Remove-Item -LiteralPath $tmpDirectory -Force
        }
    }
}
