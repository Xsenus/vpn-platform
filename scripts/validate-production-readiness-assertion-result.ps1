param(
    [Parameter(Mandatory = $true)]
    [string]$ResultJsonPath,

    [string]$ResultMarkdownPath = "",
    [switch]$RequireProductionReady,
    [switch]$WriteJson
)

$ErrorActionPreference = "Stop"

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

$resultJsonFullPath = Assert-ExistingFile -PathValue $ResultJsonPath -Label "JSON"
$result = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json

$status = [string]$result.status
if ($status -notin @("blocked", "production-ready")) {
    throw "Production readiness assertion result status must be blocked or production-ready."
}

if ($RequireProductionReady -and $status -ne "production-ready") {
    throw "Production readiness assertion result must be production-ready when -RequireProductionReady is set."
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
