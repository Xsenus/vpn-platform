param(
    [Parameter(Mandatory = $true)]
    [string]$ReportPath,
    [string]$PaymentProviderReportPath,
    [string]$AdminVpsReportPath,
    [string]$VpnLiveReportPath,
    [string]$RoadmapPath,
    [string]$ReleaseDecisionPath,
    [string]$OutputPath = "",
    [string]$JsonOutputPath = "",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param(
        [string]$PathValue,
        [string]$DefaultRelativePath
    )

    if (-not [string]::IsNullOrWhiteSpace($PathValue)) {
        if (Test-Path -LiteralPath $PathValue) {
            return (Resolve-Path -LiteralPath $PathValue).Path
        }

        throw "Required file was not found: $PathValue"
    }

    $repoRoot = Split-Path -Parent $PSScriptRoot
    $candidate = Join-Path $repoRoot $DefaultRelativePath
    if (Test-Path -LiteralPath $candidate) {
        return (Resolve-Path -LiteralPath $candidate).Path
    }

    throw "Required file was not found: $candidate"
}

function Resolve-RepoRelativePath {
    param([string]$RelativePath)

    $repoRoot = Split-Path -Parent $PSScriptRoot
    return Join-Path $repoRoot $RelativePath
}

function Get-LatestActiveReleaseId {
    $releasesPath = Resolve-RepoRelativePath "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $latest = @($releases | Where-Object { $_.isActive } | Sort-Object -Property { [DateTimeOffset]::Parse([string]$_.releasedAt) } -Descending | Select-Object -First 1)

    if ($latest.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$latest[0].releaseId)) {
        throw "Latest active release was not found in AppReleases seed."
    }

    return [string]$latest[0].releaseId
}

function Invoke-EvidenceValidator {
    param(
        [string]$Name,
        [string]$ValidatorPath,
        [string]$EvidenceReportPath
    )

    try {
        & $ValidatorPath -ReportPath $EvidenceReportPath -RequireAllPassed | Out-Host
        return [ordered]@{
            name = $Name
            status = "passed"
            reportPath = $EvidenceReportPath
            validatorPath = $ValidatorPath
            message = ""
        }
    }
    catch {
        return [ordered]@{
            name = $Name
            status = "failed"
            reportPath = $EvidenceReportPath
            validatorPath = $ValidatorPath
            message = $_.Exception.Message
        }
    }
}

function Write-Utf8NoBomFile {
    param(
        [string]$PathValue,
        [string]$Content
    )

    [System.IO.File]::WriteAllText($PathValue, $Content, [System.Text.UTF8Encoding]::new($false))
}

function ConvertTo-ReadinessMarkdown {
    param([object]$Result)

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("# Production readiness assertion")
    $lines.Add("")
    $lines.Add("- Status: ``$($Result.status)``")
    $lines.Add("- Release ID: ``$($Result.releaseId)``")
    $lines.Add("- Failed evidence reports: ``$($Result.failedEvidenceReportsCount)``")
    $lines.Add("- Blockers: ``$($Result.blockersCount)``")
    $lines.Add("- Staging/VPS report: ``$($Result.reportPath)``")
    $lines.Add("- Payment provider report: ``$($Result.paymentProviderReportPath)``")
    $lines.Add("- Admin VPS report: ``$($Result.adminVpsReportPath)``")
    $lines.Add("- VPN live report: ``$($Result.vpnLiveReportPath)``")
    $lines.Add("")
    $lines.Add("## Evidence reports")
    foreach ($report in @($Result.evidenceReports)) {
        $message = [string]$report.message
        if ([string]::IsNullOrWhiteSpace($message)) {
            $message = "ok"
        }

        $lines.Add("- ``$($report.name)``: ``$($report.status)`` - $message")
    }

    $lines.Add("")
    $lines.Add("## Blockers")
    if (@($Result.blockers).Count -eq 0) {
        $lines.Add("- none")
    }
    else {
        foreach ($blocker in @($Result.blockers)) {
            $lines.Add("- ``$blocker``")
        }
    }

    return ($lines -join [Environment]::NewLine) + [Environment]::NewLine
}

function Write-ReadinessResult {
    param([object]$Result)

    if ([string]::IsNullOrWhiteSpace($OutputPath) -and [string]::IsNullOrWhiteSpace($JsonOutputPath)) {
        return
    }

    $markdownPath = ""
    if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
        $markdownPath = [System.IO.Path]::GetFullPath($OutputPath)
        if ((Test-Path -LiteralPath $markdownPath) -and -not $Force) {
            throw "Production readiness assertion output already exists. Pass -Force to overwrite: $markdownPath"
        }

        $markdownParent = Split-Path -Parent $markdownPath
        if (-not [string]::IsNullOrWhiteSpace($markdownParent)) {
            New-Item -ItemType Directory -Path $markdownParent -Force | Out-Null
        }
    }

    $jsonPath = ""
    if (-not [string]::IsNullOrWhiteSpace($JsonOutputPath)) {
        $jsonPath = [System.IO.Path]::GetFullPath($JsonOutputPath)
    }
    elseif (-not [string]::IsNullOrWhiteSpace($markdownPath)) {
        $jsonPath = [System.IO.Path]::ChangeExtension($markdownPath, ".json")
    }

    if (-not [string]::IsNullOrWhiteSpace($jsonPath)) {
        if ((Test-Path -LiteralPath $jsonPath) -and -not $Force) {
            throw "Production readiness assertion JSON output already exists. Pass -Force to overwrite: $jsonPath"
        }

        $jsonParent = Split-Path -Parent $jsonPath
        if (-not [string]::IsNullOrWhiteSpace($jsonParent)) {
            New-Item -ItemType Directory -Path $jsonParent -Force | Out-Null
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($markdownPath)) {
        $Result.resultMarkdownPath = $markdownPath
    }

    if (-not [string]::IsNullOrWhiteSpace($jsonPath)) {
        $Result.resultJsonPath = $jsonPath
    }

    if (-not [string]::IsNullOrWhiteSpace($jsonPath)) {
        Write-Utf8NoBomFile -PathValue $jsonPath -Content ($Result | ConvertTo-Json -Depth 8)
    }

    if (-not [string]::IsNullOrWhiteSpace($markdownPath)) {
        Write-Utf8NoBomFile -PathValue $markdownPath -Content (ConvertTo-ReadinessMarkdown -Result ([pscustomobject]$Result))
    }

    if (-not [string]::IsNullOrWhiteSpace($jsonPath)) {
        $validatorArgs = @{
            ResultJsonPath = $jsonPath
            WriteJson = $true
        }

        if (-not [string]::IsNullOrWhiteSpace($markdownPath)) {
            $validatorArgs.ResultMarkdownPath = $markdownPath
        }

        if ([string]$Result.status -eq "production-ready") {
            $validatorArgs.RequireProductionReady = $true
        }

        & (Resolve-RepoRelativePath "scripts/validate-production-readiness-assertion-result.ps1") @validatorArgs | Out-Null
    }
}

$reportFullPath = Resolve-RepoPath -PathValue $ReportPath -DefaultRelativePath ""
$paymentProviderReportFullPath = Resolve-RepoPath -PathValue $PaymentProviderReportPath -DefaultRelativePath "docs/payment-provider-smoke-report.template.json"
$adminVpsReportFullPath = Resolve-RepoPath -PathValue $AdminVpsReportPath -DefaultRelativePath "docs/admin-vps-smoke-report.template.json"
$vpnLiveReportFullPath = Resolve-RepoPath -PathValue $VpnLiveReportPath -DefaultRelativePath "docs/vpn-live-smoke-report.template.json"
$roadmapFullPath = Resolve-RepoPath -PathValue $RoadmapPath -DefaultRelativePath "docs/PRODUCT_COMPLETION_ROADMAP.md"
$releaseDecisionFullPath = Resolve-RepoPath -PathValue $ReleaseDecisionPath -DefaultRelativePath "docs/release-decision.md"
$stagingValidator = Resolve-RepoPath -PathValue "" -DefaultRelativePath "scripts/validate-staging-smoke-report.ps1"
$paymentProviderValidator = Resolve-RepoPath -PathValue "" -DefaultRelativePath "scripts/validate-payment-provider-smoke-report.ps1"
$adminVpsValidator = Resolve-RepoPath -PathValue "" -DefaultRelativePath "scripts/validate-admin-vps-smoke-report.ps1"
$vpnLiveValidator = Resolve-RepoPath -PathValue "" -DefaultRelativePath "scripts/validate-vpn-live-smoke-report.ps1"

$evidenceReports = @(
    Invoke-EvidenceValidator -Name "staging-vps" -ValidatorPath $stagingValidator -EvidenceReportPath $reportFullPath
    Invoke-EvidenceValidator -Name "payment-providers" -ValidatorPath $paymentProviderValidator -EvidenceReportPath $paymentProviderReportFullPath
    Invoke-EvidenceValidator -Name "admin-vps" -ValidatorPath $adminVpsValidator -EvidenceReportPath $adminVpsReportFullPath
    Invoke-EvidenceValidator -Name "vpn-live" -ValidatorPath $vpnLiveValidator -EvidenceReportPath $vpnLiveReportFullPath
)

$roadmap = Get-Content -LiteralPath $roadmapFullPath -Raw -Encoding UTF8
$releaseDecision = Get-Content -LiteralPath $releaseDecisionFullPath -Raw -Encoding UTF8

$blockingMarkers = @(
    '[ ] `STATE-011`',
    '[ ] `STATE-012`',
    '[ ] `STATE-013`',
    '[ ] `P0-ADMIN-001`',
    '[ ] `P0-ADMIN-002`',
    '[ ] `P0-VPN-001`',
    '[ ] `P0-VPN-002`',
    '[ ] `P0-VPN-003`',
    '[ ] `P0-VPN-004`',
    '[ ] `P0-VPN-005`',
    '[ ] `P0-PAY-002`',
    '[ ] `P0-PAY-003`',
    '[ ] `P0-PAY-004`',
    '[ ] `P0-PAY-005`',
    '[ ] `P0-PAY-006`',
    '[ ] `P0-PAY-007`',
    '[ ] `P0-PAY-008`',
    '[ ] `P0-PAY-009`',
    '[ ] `P0-PAY-010`',
    '[ ] `P11-ACC-002`',
    '| BUG-001 | P0 | VPS/Admin |',
    '| BUG-002 | P0 | VPN |',
    '| BUG-003 | P0 | Payments |'
)

$foundBlockers = @()
foreach ($marker in $blockingMarkers) {
    if ($roadmap.Contains($marker)) {
        $foundBlockers += $marker
    }
}

foreach ($decisionMarker in @('staging-ready baseline', 'не production-ready', 'not production-ready')) {
    if ($releaseDecision.IndexOf($decisionMarker, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        $foundBlockers += "release-decision:$decisionMarker"
    }
}

$failedEvidenceReports = @($evidenceReports | Where-Object { $_.status -ne "passed" })
$status = if ($failedEvidenceReports.Count -gt 0 -or $foundBlockers.Count -gt 0) { "blocked" } else { "production-ready" }
$latestReleaseId = Get-LatestActiveReleaseId
$summary = [ordered]@{
    status = $status
    releaseId = $latestReleaseId
    generatedAt = [DateTimeOffset]::UtcNow.ToString("O")
    reportPath = $reportFullPath
    paymentProviderReportPath = $paymentProviderReportFullPath
    adminVpsReportPath = $adminVpsReportFullPath
    vpnLiveReportPath = $vpnLiveReportFullPath
    evidenceReports = $evidenceReports
    failedEvidenceReportsCount = $failedEvidenceReports.Count
    blockers = $foundBlockers
    blockersCount = $foundBlockers.Count
    roadmapPath = $roadmapFullPath
    releaseDecisionPath = $releaseDecisionFullPath
}

Write-ReadinessResult -Result $summary

if ($status -eq "blocked") {
    $payload = [ordered]@{
        status = "blocked"
        releaseId = $latestReleaseId
        reportPath = $reportFullPath
        paymentProviderReportPath = $paymentProviderReportFullPath
        adminVpsReportPath = $adminVpsReportFullPath
        vpnLiveReportPath = $vpnLiveReportFullPath
        evidenceReports = $evidenceReports
        failedEvidenceReportsCount = $failedEvidenceReports.Count
        blockers = $foundBlockers
        blockersCount = $foundBlockers.Count
        resultJsonPath = $summary.resultJsonPath
        resultMarkdownPath = $summary.resultMarkdownPath
    } | ConvertTo-Json -Depth 8 -Compress

    throw "Production readiness blocked: $payload"
}

Write-Output ("production readiness valid " + ($summary | ConvertTo-Json -Depth 8 -Compress))
