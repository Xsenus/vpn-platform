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
    $lines.Add("# Production evidence handoff package archive CI regression")
    $lines.Add("")
    $lines.Add("- Status: ``$($Result.status)``")
    $lines.Add("- Release: ``$($Result.releaseId)``")
    $lines.Add("- Main flow status: ``$($Result.mainFlow.status)``")
    $lines.Add("- Result validator regression: ``$($Result.resultValidatorRegression.status)``")
    $lines.Add("- Long path regression: ``$($Result.longPathRegression.status)``")
    if ($null -ne $Result.ciSummaryValidatorRegression) {
        $lines.Add("- CI summary validator regression: ``$($Result.ciSummaryValidatorRegression.status)``")
    }
    if ($null -ne $Result.ciResultValidatorRegression) {
        $lines.Add("- CI result validator regression: ``$($Result.ciResultValidatorRegression.status)``")
    }
    $lines.Add("")
    $lines.Add("## Artifacts")
    $lines.Add("- Main flow result: ``$($Result.mainFlow.resultJsonPath)``")
    $lines.Add("- Handoff package archive: ``$($Result.mainFlow.handoffPackageArchivePath)``")
    $lines.Add("- CI regression JSON: ``$($Result.resultJsonPath)``")
    $lines.Add("- CI regression Markdown: ``$($Result.resultMarkdownPath)``")

    return ($lines -join [Environment]::NewLine) + [Environment]::NewLine
}

function Add-GitHubStepSummary {
    param([string]$Markdown)

    $summaryPath = $env:GITHUB_STEP_SUMMARY
    if ([string]::IsNullOrWhiteSpace($summaryPath)) {
        return
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

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path (Resolve-RepoPath "tmp") "production-evidence-handoff-package-archive-ci-regression-test"
}

$fullOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
if ((Test-Path -LiteralPath $fullOutputDirectory) -and -not $Force) {
    throw "Production evidence handoff package archive CI regression output directory already exists. Pass -Force to overwrite: $fullOutputDirectory"
}

if (Test-Path -LiteralPath $fullOutputDirectory) {
    Remove-Item -LiteralPath $fullOutputDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $fullOutputDirectory -Force | Out-Null

$mainFlowDirectory = Join-Path $fullOutputDirectory "production-evidence-main-flow"
$longPathDirectory = Join-Path $fullOutputDirectory "production-evidence-long-release-id-path-regression"

$mainFlowJson = & (Resolve-RepoPath "scripts/test-production-evidence-handoff-package-archive-flow.ps1") `
    -OutputDirectory $mainFlowDirectory `
    -Force `
    -WriteJson
$mainFlow = $mainFlowJson | ConvertFrom-Json

if ([string]$mainFlow.status -ne "passed") {
    throw "Production evidence handoff package archive CI regression main flow did not pass."
}

$resultValidatorRegressionJson = & (Resolve-RepoPath "scripts/test-production-evidence-handoff-package-archive-flow-result-validator.ps1") `
    -ResultJsonPath ([string]$mainFlow.resultJsonPath) `
    -WriteJson
$resultValidatorRegression = $resultValidatorRegressionJson | ConvertFrom-Json

if ([string]$resultValidatorRegression.status -ne "passed") {
    throw "Production evidence handoff package archive CI regression result validator regression did not pass."
}

$longPathRegressionJson = & (Resolve-RepoPath "scripts/test-production-evidence-handoff-package-archive-long-path.ps1") `
    -OutputDirectory $longPathDirectory `
    -Force `
    -WriteJson
$longPathRegression = $longPathRegressionJson | ConvertFrom-Json

if ([string]$longPathRegression.status -ne "passed") {
    throw "Production evidence handoff package archive CI regression long path regression did not pass."
}

$resultJsonPath = Join-Path $fullOutputDirectory "production-evidence-handoff-package-archive-ci-regression-result.json"
$resultMarkdownPath = Join-Path $fullOutputDirectory "production-evidence-handoff-package-archive-ci-regression-result.md"

$result = [ordered]@{
    status = "passed"
    outputDirectory = $fullOutputDirectory
    releaseId = [string]$mainFlow.releaseId
    mainFlow = $mainFlow
    resultValidatorRegression = $resultValidatorRegression
    longPathRegression = $longPathRegression
    resultJsonPath = $resultJsonPath
    resultMarkdownPath = $resultMarkdownPath
}

$resultJson = $result | ConvertTo-Json -Depth 12
$resultMarkdown = ConvertTo-CiMarkdown -Result ([pscustomobject]$result)
Write-Utf8NoBomFile -PathValue $resultJsonPath -Content $resultJson
Write-Utf8NoBomFile -PathValue $resultMarkdownPath -Content $resultMarkdown
$githubStepSummaryPath = Add-GitHubStepSummary -Markdown $resultMarkdown

& (Resolve-RepoPath "scripts/validate-production-evidence-handoff-package-archive-ci-summary.ps1") `
    -ResultJsonPath $resultJsonPath `
    -SummaryPath $resultMarkdownPath | Out-Null

if (-not [string]::IsNullOrWhiteSpace($githubStepSummaryPath)) {
    & (Resolve-RepoPath "scripts/validate-production-evidence-handoff-package-archive-ci-summary.ps1") `
        -ResultJsonPath $resultJsonPath `
        -SummaryPath $githubStepSummaryPath | Out-Null
}

$ciSummaryValidatorRegressionJson = & (Resolve-RepoPath "scripts/test-production-evidence-handoff-package-archive-ci-summary-validator.ps1") `
    -ResultJsonPath $resultJsonPath `
    -SummaryPath $resultMarkdownPath `
    -WriteJson
$ciSummaryValidatorRegression = $ciSummaryValidatorRegressionJson | ConvertFrom-Json

if ([string]$ciSummaryValidatorRegression.status -ne "passed") {
    throw "Production evidence handoff package archive CI summary validator regression did not pass."
}

$result["ciSummaryValidatorRegression"] = $ciSummaryValidatorRegression
$resultJson = $result | ConvertTo-Json -Depth 12
$resultMarkdown = ConvertTo-CiMarkdown -Result ([pscustomobject]$result)
Write-Utf8NoBomFile -PathValue $resultJsonPath -Content $resultJson
Write-Utf8NoBomFile -PathValue $resultMarkdownPath -Content $resultMarkdown

if (-not [string]::IsNullOrWhiteSpace($githubStepSummaryPath)) {
    Write-Utf8NoBomFile -PathValue $githubStepSummaryPath -Content $resultMarkdown
    & (Resolve-RepoPath "scripts/validate-production-evidence-handoff-package-archive-ci-summary.ps1") `
        -ResultJsonPath $resultJsonPath `
        -SummaryPath $githubStepSummaryPath | Out-Null
}

& (Resolve-RepoPath "scripts/validate-production-evidence-handoff-package-archive-ci-regression-result.ps1") `
    -ResultJsonPath $resultJsonPath `
    -ResultMarkdownPath $resultMarkdownPath | Out-Null

$ciResultValidatorRegressionJson = & (Resolve-RepoPath "scripts/test-production-evidence-handoff-package-archive-ci-regression-result-validator.ps1") `
    -ResultJsonPath $resultJsonPath `
    -ResultMarkdownPath $resultMarkdownPath `
    -WriteJson
$ciResultValidatorRegression = $ciResultValidatorRegressionJson | ConvertFrom-Json

if ([string]$ciResultValidatorRegression.status -ne "passed") {
    throw "Production evidence handoff package archive CI regression result validator regression did not pass."
}

$result["ciResultValidatorRegression"] = $ciResultValidatorRegression
$resultJson = $result | ConvertTo-Json -Depth 12
$resultMarkdown = ConvertTo-CiMarkdown -Result ([pscustomobject]$result)
Write-Utf8NoBomFile -PathValue $resultJsonPath -Content $resultJson
Write-Utf8NoBomFile -PathValue $resultMarkdownPath -Content $resultMarkdown

if (-not [string]::IsNullOrWhiteSpace($githubStepSummaryPath)) {
    Write-Utf8NoBomFile -PathValue $githubStepSummaryPath -Content $resultMarkdown
}

& (Resolve-RepoPath "scripts/validate-production-evidence-handoff-package-archive-ci-regression-result.ps1") `
    -ResultJsonPath $resultJsonPath `
    -ResultMarkdownPath $resultMarkdownPath | Out-Null

if ($WriteJson) {
    Write-Output $resultJson
}
else {
    Write-Host "production evidence handoff package archive CI regression passed $($result | ConvertTo-Json -Depth 12 -Compress)"
}
