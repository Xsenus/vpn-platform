param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$ReportPath,

    [Parameter(Mandatory = $true)]
    [string]$PaymentProviderReportPath,

    [Parameter(Mandatory = $true)]
    [string]$AdminVpsReportPath,

    [Parameter(Mandatory = $true)]
    [string]$VpnLiveReportPath,

    [string]$JsonOutputPath = "",
    [string]$RoadmapPath = "",
    [string]$ReleaseDecisionPath = "",
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

function Read-JsonFile {
    param([string]$Path)

    return Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
}

function Get-StatusCounts {
    param([object[]]$Items)

    $counts = [ordered]@{
        total = @($Items).Count
        passed = 0
        failed = 0
        blocked = 0
        skipped = 0
        other = 0
    }

    foreach ($item in @($Items)) {
        $status = ([string]$item.status).Trim().ToLowerInvariant()
        switch ($status) {
            "passed" { $counts.passed++ }
            "failed" { $counts.failed++ }
            "blocked" { $counts.blocked++ }
            "skipped" { $counts.skipped++ }
            default { $counts.other++ }
        }
    }

    return $counts
}

function Get-BooleanCounts {
    param([hashtable]$Flags)

    $passed = 0
    foreach ($value in $Flags.Values) {
        if ($value -eq $true) {
            $passed++
        }
    }

    return [ordered]@{
        total = $Flags.Count
        passed = $passed
        blocked = $Flags.Count - $passed
    }
}

function Get-ReportStatus {
    param(
        [hashtable]$BooleanFlags,
        [object[]]$Items
    )

    $statusCounts = Get-StatusCounts -Items $Items
    $booleanCounts = Get-BooleanCounts -Flags $BooleanFlags
    if ($statusCounts.total -eq $statusCounts.passed -and $booleanCounts.total -eq $booleanCounts.passed) {
        return "passed"
    }

    if ($statusCounts.failed -gt 0 -or $statusCounts.other -gt 0) {
        return "failed"
    }

    return "blocked"
}

function Get-RoadmapBlockers {
    param(
        [string]$RoadmapFullPath,
        [string]$ReleaseDecisionFullPath
    )

    $roadmap = Get-Content -LiteralPath $RoadmapFullPath -Raw -Encoding UTF8
    $releaseDecision = Get-Content -LiteralPath $ReleaseDecisionFullPath -Raw -Encoding UTF8
    $blockingMarkers = @(
        '[ ] `STATE-011`',
        '[ ] `STATE-012`',
        '[ ] `STATE-013`',
        '[ ] `P0-ADMIN-001`',
        '[ ] `P0-ADMIN-002`',
        '[ ] `P0-VPN-001`',
        '[ ] `P0-VPN-002`',
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
        '[ ] `P11-ACC-002`',
        '| BUG-001 | P0 | VPS/Admin |',
        '| BUG-002 | P0 | VPN |',
        '| BUG-003 | P0 | Payments |'
    )

    $found = @()
    foreach ($marker in $blockingMarkers) {
        if ($roadmap.Contains($marker)) {
            $found += $marker
        }
    }

    foreach ($decisionMarker in @("staging-ready baseline", "not production-ready")) {
        if ($releaseDecision.IndexOf($decisionMarker, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $found += "release-decision:$decisionMarker"
        }
    }

    return $found
}

function Format-StatusLine {
    param(
        [string]$Name,
        [hashtable]$BooleanFlags,
        [object[]]$Items
    )

    $status = Get-ReportStatus -BooleanFlags $BooleanFlags -Items $Items
    $statusCounts = Get-StatusCounts -Items $Items
    $booleanCounts = Get-BooleanCounts -Flags $BooleanFlags
    return "| $Name | $status | checks $($statusCounts.passed)/$($statusCounts.total) | flags $($booleanCounts.passed)/$($booleanCounts.total) |"
}

$stagingReportFullPath = Resolve-RepoPath -PathValue $ReportPath -DefaultRelativePath ""
$paymentProviderReportFullPath = Resolve-RepoPath -PathValue $PaymentProviderReportPath -DefaultRelativePath ""
$adminVpsReportFullPath = Resolve-RepoPath -PathValue $AdminVpsReportPath -DefaultRelativePath ""
$vpnLiveReportFullPath = Resolve-RepoPath -PathValue $VpnLiveReportPath -DefaultRelativePath ""
$roadmapFullPath = Resolve-RepoPath -PathValue $RoadmapPath -DefaultRelativePath "docs/PRODUCT_COMPLETION_ROADMAP.md"
$releaseDecisionFullPath = Resolve-RepoPath -PathValue $ReleaseDecisionPath -DefaultRelativePath "docs/release-decision.md"
$fullOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
$fullJsonOutputPath = if ([string]::IsNullOrWhiteSpace($JsonOutputPath)) {
    [System.IO.Path]::ChangeExtension($fullOutputPath, ".json")
} else {
    [System.IO.Path]::GetFullPath($JsonOutputPath)
}

foreach ($path in @($fullOutputPath, $fullJsonOutputPath)) {
    if ((Test-Path -LiteralPath $path) -and -not $Force) {
        throw "Output file already exists. Pass -Force to overwrite: $path"
    }

    $parent = Split-Path -Parent $path
    if (-not [string]::IsNullOrWhiteSpace($parent) -and -not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Path $parent | Out-Null
    }
}

$staging = Read-JsonFile -Path $stagingReportFullPath
$payments = Read-JsonFile -Path $paymentProviderReportFullPath
$adminVps = Read-JsonFile -Path $adminVpsReportFullPath
$vpnLive = Read-JsonFile -Path $vpnLiveReportFullPath

$stagingFlags = @{}
$paymentFlags = @{}
$adminFlags = @{
    accountBootstrapChecked = $adminVps.accountBootstrapChecked
    adminLoginPassed = $adminVps.adminLoginPassed
    noJsErrors = $adminVps.noJsErrors
    noUnauthorizedAfterLogin = $adminVps.noUnauthorizedAfterLogin
}
$vpnFlags = @{
    panelConnected = $vpnLive.panelConnected
    inboundSynced = $vpnLive.inboundSynced
    nodeReady = $vpnLive.nodeReady
    productionProvisioningEnabled = $vpnLive.productionProvisioningEnabled
    noSandboxFallback = $vpnLive.noSandboxFallback
    failClosedChecked = $vpnLive.failClosedChecked
}

$reports = @(
    [ordered]@{
        name = "staging-vps"
        status = Get-ReportStatus -BooleanFlags $stagingFlags -Items @($staging.checks)
        path = $stagingReportFullPath
        counts = Get-StatusCounts -Items @($staging.checks)
        flagCounts = Get-BooleanCounts -Flags $stagingFlags
    },
    [ordered]@{
        name = "payment-providers"
        status = Get-ReportStatus -BooleanFlags $paymentFlags -Items @($payments.providers)
        path = $paymentProviderReportFullPath
        counts = Get-StatusCounts -Items @($payments.providers)
        flagCounts = Get-BooleanCounts -Flags $paymentFlags
    },
    [ordered]@{
        name = "admin-vps"
        status = Get-ReportStatus -BooleanFlags $adminFlags -Items @($adminVps.sections)
        path = $adminVpsReportFullPath
        counts = Get-StatusCounts -Items @($adminVps.sections)
        flagCounts = Get-BooleanCounts -Flags $adminFlags
    },
    [ordered]@{
        name = "vpn-live"
        status = Get-ReportStatus -BooleanFlags $vpnFlags -Items @($vpnLive.checks)
        path = $vpnLiveReportFullPath
        counts = Get-StatusCounts -Items @($vpnLive.checks)
        flagCounts = Get-BooleanCounts -Flags $vpnFlags
    }
)

$roadmapBlockers = @(Get-RoadmapBlockers -RoadmapFullPath $roadmapFullPath -ReleaseDecisionFullPath $releaseDecisionFullPath)
$failedReports = @($reports | Where-Object { $_.status -ne "passed" })
$status = if ($failedReports.Count -eq 0 -and $roadmapBlockers.Count -eq 0) { "production-ready" } else { "blocked" }
$releaseId = [string]$staging.releaseId
if ([string]::IsNullOrWhiteSpace($releaseId)) {
    $releaseId = [string]$payments.releaseId
}

$summary = [ordered]@{
    status = $status
    releaseId = $releaseId
    generatedAt = [DateTimeOffset]::UtcNow.ToString("o")
    summaryPath = $fullOutputPath
    jsonSummaryPath = $fullJsonOutputPath
    reports = $reports
    roadmapBlockers = $roadmapBlockers
    reportPaths = [ordered]@{
        staging = $stagingReportFullPath
        paymentProviders = $paymentProviderReportFullPath
        adminVps = $adminVpsReportFullPath
        vpnLive = $vpnLiveReportFullPath
    }
    roadmapPath = $roadmapFullPath
    releaseDecisionPath = $releaseDecisionFullPath
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# Production readiness summary")
$lines.Add("")
$lines.Add(("- Status: ``{0}``" -f $status))
$lines.Add(("- Release: ``{0}``" -f $releaseId))
$lines.Add(("- Generated at: ``{0}``" -f $summary.generatedAt))
$lines.Add("")
$lines.Add("## Evidence reports")
$lines.Add("")
$lines.Add("| Report | Status | Evidence checks | Required flags |")
$lines.Add("| --- | --- | --- | --- |")
$lines.Add((Format-StatusLine -Name "staging-vps" -BooleanFlags $stagingFlags -Items @($staging.checks)))
$lines.Add((Format-StatusLine -Name "payment-providers" -BooleanFlags $paymentFlags -Items @($payments.providers)))
$lines.Add((Format-StatusLine -Name "admin-vps" -BooleanFlags $adminFlags -Items @($adminVps.sections)))
$lines.Add((Format-StatusLine -Name "vpn-live" -BooleanFlags $vpnFlags -Items @($vpnLive.checks)))
$lines.Add("")
$lines.Add("## Payment providers")
$lines.Add("")
foreach ($provider in @($payments.providers)) {
    $lines.Add(("- ``{0}``: ``{1}`` ({2})" -f $provider.provider, $provider.status, $provider.mode))
}
$lines.Add("")
$lines.Add("## Roadmap blockers")
$lines.Add("")
if ($roadmapBlockers.Count -eq 0) {
    $lines.Add("- None")
} else {
    foreach ($blocker in $roadmapBlockers) {
        $lines.Add(("- {0}" -f $blocker))
    }
}
$lines.Add("")
$lines.Add("## Safety")
$lines.Add("")
$lines.Add("- Do not paste provider secrets, cookies, auth headers, private keys or full VPN access URIs into evidence files.")
$lines.Add("- Use sanitized ids, timestamps, section names and screenshot file names only.")

Set-Content -LiteralPath $fullOutputPath -Value $lines -Encoding UTF8
Set-Content -LiteralPath $fullJsonOutputPath -Value ($summary | ConvertTo-Json -Depth 10) -Encoding UTF8

Write-Output "production readiness summary generated $fullOutputPath status=$status"
