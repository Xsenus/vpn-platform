param(
    [Parameter(Mandatory = $true)]
    [string]$SummaryPath,

    [string]$JsonSummaryPath = "",
    [switch]$RequireProductionReady,
    [switch]$RequireReportFiles
)

$ErrorActionPreference = "Stop"

function Resolve-RequiredPath {
    param([string]$PathValue)

    if ([string]::IsNullOrWhiteSpace($PathValue) -or -not (Test-Path -LiteralPath $PathValue)) {
        throw "Production readiness summary file was not found: $PathValue"
    }

    return (Resolve-Path -LiteralPath $PathValue).Path
}

function Assert-StringField {
    param(
        [object]$Object,
        [string]$PropertyName,
        [string]$Context
    )

    if (-not $Object.PSObject.Properties.Name.Contains($PropertyName)) {
        throw "$Context is missing required field: $PropertyName"
    }

    if ([string]::IsNullOrWhiteSpace([string]$Object.$PropertyName)) {
        throw "$Context field is empty: $PropertyName"
    }
}

function Assert-CountObject {
    param(
        [object]$Counts,
        [string]$Context,
        [string[]]$Properties
    )

    foreach ($propertyName in $Properties) {
        if (-not $Counts.PSObject.Properties.Name.Contains($propertyName)) {
            throw "$Context is missing count field: $propertyName"
        }

        $value = 0
        if (-not [int]::TryParse([string]$Counts.$propertyName, [ref]$value) -or $value -lt 0) {
            throw "$Context count field $propertyName must be a non-negative integer."
        }
    }
}

$summaryFullPath = Resolve-RequiredPath -PathValue $SummaryPath
$jsonSummaryFullPath = if ([string]::IsNullOrWhiteSpace($JsonSummaryPath)) {
    [System.IO.Path]::ChangeExtension($summaryFullPath, ".json")
} else {
    $JsonSummaryPath
}
$jsonSummaryFullPath = Resolve-RequiredPath -PathValue $jsonSummaryFullPath

$secretMarkers = @(
    "password=",
    "authorization:",
    "bearer ",
    "cookie:",
    "set-cookie:",
    ".env",
    "client_secret",
    "api_key",
    "x-api-key",
    "secretkey",
    "webhook secret",
    "vps_ssh_key",
    "x-telegram-bot-api-secret-token",
    "vless://",
    "vmess://",
    "trojan://",
    "begin private key",
    "begin rsa private key",
    "begin openssh private key"
)

$markdown = Get-Content -LiteralPath $summaryFullPath -Raw -Encoding UTF8
$jsonRaw = Get-Content -LiteralPath $jsonSummaryFullPath -Raw -Encoding UTF8
foreach ($raw in @($markdown, $jsonRaw)) {
    if ($raw.Contains([char]0xFFFD)) {
        throw "Production readiness summary contains invalid UTF-8 replacement character."
    }

    $lowerRaw = $raw.ToLowerInvariant()
    foreach ($marker in $secretMarkers) {
        if ($lowerRaw.Contains($marker)) {
            throw "Production readiness summary contains forbidden secret marker: $marker"
        }
    }
}

foreach ($requiredText in @(
        "# Production readiness summary",
        "## Evidence reports",
        "## Payment providers",
        "## Roadmap blockers",
        "## Safety",
        "staging-vps",
        "payment-providers",
        "admin-vps",
        "vpn-live")) {
    if ($markdown.IndexOf($requiredText, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Production readiness summary markdown is missing required text: $requiredText"
    }
}

try {
    $summary = $jsonRaw | ConvertFrom-Json
}
catch {
    throw "Production readiness summary JSON is invalid: $($_.Exception.Message)"
}

foreach ($fieldName in @("status", "releaseId", "generatedAt", "roadmapPath", "releaseDecisionPath")) {
    Assert-StringField -Object $summary -PropertyName $fieldName -Context "Production readiness summary"
}

if (@("blocked", "production-ready") -notcontains ([string]$summary.status)) {
    throw "Production readiness summary status is unsupported: $($summary.status)"
}

$generatedAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$summary.generatedAt, [ref]$generatedAt)) {
    throw "Production readiness summary generatedAt is not a valid DateTimeOffset."
}

if ($null -eq $summary.reportPaths) {
    throw "Production readiness summary is missing reportPaths."
}

foreach ($propertyName in @("staging", "paymentProviders", "adminVps", "vpnLive")) {
    Assert-StringField -Object $summary.reportPaths -PropertyName $propertyName -Context "Production readiness summary reportPaths"

    if ($RequireReportFiles -and -not (Test-Path -LiteralPath ([string]$summary.reportPaths.$propertyName))) {
        throw "Production readiness summary referenced report file was not found: $($summary.reportPaths.$propertyName)"
    }
}

if ($null -eq $summary.reports -or @($summary.reports).Count -ne 4) {
    throw "Production readiness summary must contain exactly four reports."
}

$allowedStatuses = @("passed", "failed", "blocked")
$requiredReportNames = @("staging-vps", "payment-providers", "admin-vps", "vpn-live")
$reportNames = @($summary.reports | ForEach-Object { [string]$_.name })
foreach ($reportName in $requiredReportNames) {
    if ($reportNames -notcontains $reportName) {
        throw "Production readiness summary is missing report: $reportName"
    }
}

$duplicates = $reportNames | Group-Object | Where-Object { $_.Count -gt 1 }
if ($duplicates) {
    throw "Production readiness summary contains duplicated report: $($duplicates[0].Name)"
}

$nonPassedReports = @()
foreach ($report in @($summary.reports)) {
    Assert-StringField -Object $report -PropertyName "name" -Context "Production readiness summary report"
    Assert-StringField -Object $report -PropertyName "status" -Context "Production readiness summary report $($report.name)"
    Assert-StringField -Object $report -PropertyName "path" -Context "Production readiness summary report $($report.name)"

    if ($requiredReportNames -notcontains ([string]$report.name)) {
        throw "Production readiness summary contains unsupported report: $($report.name)"
    }

    if ($allowedStatuses -notcontains ([string]$report.status)) {
        throw "Production readiness summary report $($report.name) has unsupported status: $($report.status)"
    }

    Assert-CountObject -Counts $report.counts -Context "Production readiness summary report $($report.name) counts" -Properties @("total", "passed", "failed", "blocked", "skipped", "other")
    Assert-CountObject -Counts $report.flagCounts -Context "Production readiness summary report $($report.name) flagCounts" -Properties @("total", "passed", "blocked")

    $checkTotal = [int]$report.counts.total
    $checkSum = [int]$report.counts.passed + [int]$report.counts.failed + [int]$report.counts.blocked + [int]$report.counts.skipped + [int]$report.counts.other
    if ($checkTotal -ne $checkSum) {
        throw "Production readiness summary report $($report.name) count total does not match status sum."
    }

    $flagTotal = [int]$report.flagCounts.total
    $flagSum = [int]$report.flagCounts.passed + [int]$report.flagCounts.blocked
    if ($flagTotal -ne $flagSum) {
        throw "Production readiness summary report $($report.name) flag total does not match passed+blocked."
    }

    if ($RequireReportFiles -and -not (Test-Path -LiteralPath ([string]$report.path))) {
        throw "Production readiness summary referenced report file was not found: $($report.path)"
    }

    if ([string]$report.status -ne "passed") {
        $nonPassedReports += [string]$report.name
    }
}

$roadmapBlockers = @($summary.roadmapBlockers)
if ([string]$summary.status -eq "blocked" -and $nonPassedReports.Count -eq 0 -and $roadmapBlockers.Count -eq 0) {
    throw "Production readiness summary is blocked but has no non-passed reports or roadmap blockers."
}

if ($RequireProductionReady) {
    if ([string]$summary.status -ne "production-ready") {
        throw "Production readiness summary must be production-ready when -RequireProductionReady is used."
    }

    if ($nonPassedReports.Count -gt 0) {
        throw "Production readiness summary has non-passed reports: $($nonPassedReports -join ', ')"
    }

    if ($roadmapBlockers.Count -gt 0) {
        throw "Production readiness summary has roadmap blockers: $($roadmapBlockers -join ', ')"
    }
}

$result = [ordered]@{
    status = $summary.status
    releaseId = $summary.releaseId
    reports = @($summary.reports).Count
    nonPassedReports = $nonPassedReports.Count
    roadmapBlockers = $roadmapBlockers.Count
}

Write-Host "production readiness summary valid $($result | ConvertTo-Json -Compress)"
