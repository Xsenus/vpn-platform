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
        throw "Production readiness assertion result $Label was not found: $PathValue"
    }

    return (Resolve-Path -LiteralPath $PathValue).Path
}

function Assert-MarkdownContains {
    param(
        [string]$Markdown,
        [string]$Expected
    )

    if ($Markdown.IndexOf($Expected, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Production readiness assertion result markdown is missing: $Expected"
    }
}

function Resolve-LinkedPath {
    param([string]$PathValue)

    if ([string]::IsNullOrWhiteSpace($PathValue)) {
        return ""
    }

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return [System.IO.Path]::GetFullPath($PathValue)
    }

    $repoRoot = Split-Path -Parent $PSScriptRoot
    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $PathValue))
}

function Assert-SamePath {
    param(
        [string]$Actual,
        [string]$Expected,
        [string]$FieldName
    )

    if (-not [string]::Equals($Actual, $Expected, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Production readiness assertion result $FieldName must match validated result path."
    }
}

$resultJsonFullPath = Assert-ExistingFile -PathValue $ResultJsonPath -Label "JSON"
$result = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ([string]::IsNullOrWhiteSpace([string]$result.resultJsonPath)) {
    throw "Production readiness assertion result resultJsonPath is required."
}

Assert-SamePath -Actual (Resolve-LinkedPath ([string]$result.resultJsonPath)) -Expected $resultJsonFullPath -FieldName "resultJsonPath"

$status = [string]$result.status
if ($status -notin @("blocked", "production-ready")) {
    throw "Production readiness assertion result status must be blocked or production-ready."
}

if ($RequireProductionReady -and $status -ne "production-ready") {
    throw "Production readiness assertion result must be production-ready when -RequireProductionReady is set."
}

$releaseId = [string]$result.releaseId
if ([string]::IsNullOrWhiteSpace($releaseId)) {
    throw "Production readiness assertion result releaseId is required."
}

if ($RequireProductionReady) {
    $latestReleaseId = Get-LatestActiveReleaseId
    if (-not [string]::Equals($releaseId, $latestReleaseId, [System.StringComparison]::Ordinal)) {
        throw "Production readiness assertion result releaseId '$releaseId' must match latest active release '$latestReleaseId' when -RequireProductionReady is used."
    }
}

foreach ($pathProperty in @(
        [string]$result.reportPath,
        [string]$result.paymentProviderReportPath,
        [string]$result.adminVpsReportPath,
        [string]$result.vpnLiveReportPath,
        [string]$result.roadmapPath,
        [string]$result.releaseDecisionPath
    )) {
    Assert-ExistingFile -PathValue $pathProperty -Label "linked file" | Out-Null
}

$evidenceReports = @($result.evidenceReports)
foreach ($expectedReport in @("staging-vps", "payment-providers", "admin-vps", "vpn-live")) {
    $report = $evidenceReports | Where-Object { [string]$_.name -eq $expectedReport } | Select-Object -First 1
    if ($null -eq $report) {
        throw "Production readiness assertion result is missing evidence report: $expectedReport"
    }

    if ([string]$report.status -notin @("passed", "failed")) {
        throw "Production readiness assertion result evidence report status must be passed or failed: $expectedReport"
    }

    Assert-ExistingFile -PathValue ([string]$report.reportPath) -Label "$expectedReport report" | Out-Null
    Assert-ExistingFile -PathValue ([string]$report.validatorPath) -Label "$expectedReport validator" | Out-Null
}

$failedEvidenceReportsCount = [int]$result.failedEvidenceReportsCount
$actualFailedEvidenceReportsCount = @($evidenceReports | Where-Object { [string]$_.status -ne "passed" }).Count
if ($failedEvidenceReportsCount -ne $actualFailedEvidenceReportsCount) {
    throw "Production readiness assertion result failedEvidenceReportsCount does not match evidenceReports."
}

$blockers = @($result.blockers)
$blockersCount = [int]$result.blockersCount
if ($blockersCount -ne $blockers.Count) {
    throw "Production readiness assertion result blockersCount does not match blockers."
}

if ($status -eq "blocked" -and $failedEvidenceReportsCount -eq 0 -and $blockersCount -eq 0) {
    throw "Production readiness assertion result blocked status requires failed evidence reports or blockers."
}

if ($status -eq "production-ready" -and ($failedEvidenceReportsCount -ne 0 -or $blockersCount -ne 0)) {
    throw "Production readiness assertion result production-ready status requires zero failed evidence reports and zero blockers."
}

if ([string]::IsNullOrWhiteSpace($ResultMarkdownPath)) {
    $ResultMarkdownPath = [string]$result.resultMarkdownPath
}

$resultMarkdownFullPath = ""
if (-not [string]::IsNullOrWhiteSpace($ResultMarkdownPath)) {
    $resultMarkdownFullPath = Assert-ExistingFile -PathValue $ResultMarkdownPath -Label "Markdown"
    if (-not [string]::IsNullOrWhiteSpace([string]$result.resultMarkdownPath)) {
        Assert-SamePath -Actual (Resolve-LinkedPath ([string]$result.resultMarkdownPath)) -Expected $resultMarkdownFullPath -FieldName "resultMarkdownPath"
    }

    $markdown = Get-Content -LiteralPath $resultMarkdownFullPath -Raw -Encoding UTF8
    foreach ($expected in @(
            "# Production readiness assertion",
            "- Status: ``$status``",
            "- Failed evidence reports: ``$failedEvidenceReportsCount``",
            "- Blockers: ``$blockersCount``",
            "## Evidence reports",
            "``staging-vps``",
            "``payment-providers``",
            "``admin-vps``",
            "``vpn-live``",
            "## Blockers"
        )) {
        Assert-MarkdownContains -Markdown $markdown -Expected $expected
    }
}

$validation = [ordered]@{
    status = "valid"
    assertionStatus = $status
    releaseId = $releaseId
    resultJsonPath = $resultJsonFullPath
    resultMarkdownPath = $resultMarkdownFullPath
    failedEvidenceReportsCount = $failedEvidenceReportsCount
    blockersCount = $blockersCount
    evidenceReportsCount = $evidenceReports.Count
}

if ($WriteJson) {
    Write-Output ($validation | ConvertTo-Json -Depth 6)
}
else {
    Write-Host "production readiness assertion result valid $($validation | ConvertTo-Json -Depth 6 -Compress)"
}
