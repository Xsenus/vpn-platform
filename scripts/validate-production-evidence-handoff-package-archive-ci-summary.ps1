param(
    [Parameter(Mandatory = $true)]
    [string]$ResultJsonPath,

    [Parameter(Mandatory = $true)]
    [string]$SummaryPath,

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
        throw "Production evidence handoff package archive CI summary $Label was not found: $PathValue"
    }

    return (Resolve-Path -LiteralPath $PathValue).Path
}

function Assert-Status {
    param(
        [object]$Value,
        [string]$Label
    )

    if ([string]$Value -ne "passed") {
        throw "Production evidence handoff package archive CI summary $Label must be passed."
    }
}

function Assert-MarkdownContains {
    param(
        [string]$Markdown,
        [string]$Expected
    )

    if ($Markdown.IndexOf($Expected, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Production evidence handoff package archive CI summary markdown is missing: $Expected"
    }
}

$resultJsonFullPath = Assert-ExistingFile -PathValue $ResultJsonPath -Label "JSON"
$summaryFullPath = Assert-ExistingFile -PathValue $SummaryPath -Label "Markdown"

$result = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
$summary = Get-Content -LiteralPath $summaryFullPath -Raw -Encoding UTF8

Assert-Status -Value $result.status -Label "status"
Assert-Status -Value $result.mainFlow.status -Label "main flow status"
Assert-Status -Value $result.resultValidatorRegression.status -Label "result validator regression status"
Assert-Status -Value $result.longPathRegression.status -Label "long path regression status"

$releaseId = [string]$result.releaseId
if ([string]::IsNullOrWhiteSpace($releaseId)) {
    throw "Production evidence handoff package archive CI summary releaseId is required."
}

if ($RequireProductionReady) {
    $latestReleaseId = Get-LatestActiveReleaseId
    if (-not [string]::Equals($releaseId, $latestReleaseId, [System.StringComparison]::Ordinal)) {
        throw "Production evidence handoff package archive CI summary releaseId '$releaseId' must match latest active release '$latestReleaseId' when -RequireProductionReady is used."
    }
}

foreach ($expected in @(
        "# Production evidence handoff package archive CI regression",
        "- Status: ``passed``",
        "- Release: ``$releaseId``",
        "- Main flow status: ``passed``",
        "- Result validator regression: ``passed``",
        "- Long path regression: ``passed``",
        "## Artifacts",
        "Main flow result",
        "Handoff package archive",
        "CI regression JSON",
        "CI regression Markdown"
    )) {
    Assert-MarkdownContains -Markdown $summary -Expected $expected
}

foreach ($pathProperty in @(
        [string]$result.mainFlow.resultJsonPath,
        [string]$result.mainFlow.handoffPackageArchivePath,
        [string]$result.resultJsonPath,
        [string]$result.resultMarkdownPath
    )) {
    if ([string]::IsNullOrWhiteSpace($pathProperty)) {
        throw "Production evidence handoff package archive CI summary artifact path is required."
    }

    Assert-MarkdownContains -Markdown $summary -Expected $pathProperty
}

$validation = [ordered]@{
    status = "valid"
    releaseId = $releaseId
    resultJsonPath = $resultJsonFullPath
    summaryPath = $summaryFullPath
    mainFlowStatus = [string]$result.mainFlow.status
    resultValidatorRegressionStatus = [string]$result.resultValidatorRegression.status
    longPathRegressionStatus = [string]$result.longPathRegression.status
}

if ($WriteJson) {
    Write-Output ($validation | ConvertTo-Json -Depth 6)
}
else {
    Write-Host "production evidence handoff package archive CI summary valid $($validation | ConvertTo-Json -Depth 6 -Compress)"
}
