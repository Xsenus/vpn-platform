param(
    [Parameter(Mandatory = $true)]
    [string]$ResultJsonPath,

    [string]$ResultMarkdownPath = "",
    [switch]$RequireProductionReady,
    [switch]$WriteJson
)

$ErrorActionPreference = "Stop"

if ((Get-Command ConvertFrom-Json).Parameters.ContainsKey("DateKind")) {
    $PSDefaultParameterValues["ConvertFrom-Json:DateKind"] = "String"
}

function Resolve-RepoPath {
    param([string]$RelativePath)

    $repoRoot = Split-Path -Parent $PSScriptRoot
    return Join-Path $repoRoot $RelativePath
}

function Get-LatestActiveReleaseId {
    $releasesPath = Resolve-RepoPath "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $latest = @($releases | Where-Object { $_.isActive } | Sort-Object -Property { [System.DateTimeOffset]::Parse([string]$_.releasedAt, [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::RoundtripKind) } -Descending | Select-Object -First 1)

    if ($latest.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$latest[0].releaseId)) {
        throw "Latest active release was not found in AppReleases seed."
    }

    return [string]$latest[0].releaseId
}

function Assert-ExistingFile {
    param(
        [string]$PathValue,
        [string]$Label
    )

    if ([string]::IsNullOrWhiteSpace($PathValue) -or -not (Test-Path -LiteralPath $PathValue -PathType Leaf)) {
        throw "Production evidence handoff package archive CI regression result $Label was not found: $PathValue"
    }

    return (Resolve-Path -LiteralPath $PathValue).Path
}

function Assert-Passed {
    param(
        [object]$Value,
        [string]$Label
    )

    if ([string]$Value -ne "passed") {
        throw "Production evidence handoff package archive CI regression result $Label must be passed."
    }
}

function Assert-MarkdownContains {
    param(
        [string]$Markdown,
        [string]$Expected
    )

    if ($Markdown.IndexOf($Expected, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Production evidence handoff package archive CI regression result markdown is missing: $Expected"
    }
}

$resultJsonFullPath = Assert-ExistingFile -PathValue $ResultJsonPath -Label "JSON"
$result = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json

Assert-Passed -Value $result.status -Label "status"
Assert-Passed -Value $result.mainFlow.status -Label "main flow status"
Assert-Passed -Value $result.resultValidatorRegression.status -Label "result validator regression status"
Assert-Passed -Value $result.longPathRegression.status -Label "long path regression status"
Assert-Passed -Value $result.ciSummaryValidatorRegression.status -Label "CI summary validator regression status"

$releaseId = [string]$result.releaseId
if ([string]::IsNullOrWhiteSpace($releaseId)) {
    throw "Production evidence handoff package archive CI regression result releaseId is required."
}

if ($RequireProductionReady) {
    $latestReleaseId = Get-LatestActiveReleaseId
    if (-not [string]::Equals($releaseId, $latestReleaseId, [System.StringComparison]::Ordinal)) {
        throw "Production evidence handoff package archive CI regression result releaseId '$releaseId' must match latest active release '$latestReleaseId' when -RequireProductionReady is used."
    }
}

if ([string]::IsNullOrWhiteSpace($ResultMarkdownPath)) {
    $ResultMarkdownPath = [string]$result.resultMarkdownPath
}

$resultMarkdownFullPath = Assert-ExistingFile -PathValue $ResultMarkdownPath -Label "Markdown"

foreach ($pathProperty in @(
        [string]$result.mainFlow.resultJsonPath,
        [string]$result.mainFlow.handoffPackageArchivePath,
        [string]$result.resultJsonPath,
        [string]$result.resultMarkdownPath
    )) {
    Assert-ExistingFile -PathValue $pathProperty -Label "artifact" | Out-Null
}

$testedFailures = @($result.ciSummaryValidatorRegression.testedFailures)
foreach ($expectedFailure in @("bad-main-flow-status", "bad-release-summary", "missing-artifact-path", "bad-long-path-status")) {
    if (-not ($testedFailures | Where-Object { [string]$_.name -eq $expectedFailure })) {
        throw "Production evidence handoff package archive CI regression result is missing summary validator failure: $expectedFailure"
    }
}

$markdown = Get-Content -LiteralPath $resultMarkdownFullPath -Raw -Encoding UTF8
foreach ($expected in @(
        "# Production evidence handoff package archive CI regression",
        "- Status: ``passed``",
        "- Release: ``$releaseId``",
        "- Main flow status: ``passed``",
        "- Result validator regression: ``passed``",
        "- Long path regression: ``passed``",
        "- CI summary validator regression: ``passed``",
        "## Artifacts",
        [string]$result.mainFlow.resultJsonPath,
        [string]$result.mainFlow.handoffPackageArchivePath,
        [string]$result.resultJsonPath,
        [string]$result.resultMarkdownPath
    )) {
    Assert-MarkdownContains -Markdown $markdown -Expected $expected
}

$validation = [ordered]@{
    status = "valid"
    releaseId = $releaseId
    resultJsonPath = $resultJsonFullPath
    resultMarkdownPath = $resultMarkdownFullPath
    mainFlowStatus = [string]$result.mainFlow.status
    resultValidatorRegressionStatus = [string]$result.resultValidatorRegression.status
    longPathRegressionStatus = [string]$result.longPathRegression.status
    ciSummaryValidatorRegressionStatus = [string]$result.ciSummaryValidatorRegression.status
    summaryValidatorTestedFailuresCount = $testedFailures.Count
}

if ($WriteJson) {
    Write-Output ($validation | ConvertTo-Json -Depth 6)
}
else {
    Write-Host "production evidence handoff package archive CI regression result valid $($validation | ConvertTo-Json -Depth 6 -Compress)"
}
