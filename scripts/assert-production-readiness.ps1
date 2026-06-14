param(
    [Parameter(Mandatory = $true)]
    [string]$ReportPath,
    [string]$RoadmapPath,
    [string]$ReleaseDecisionPath
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

$reportFullPath = Resolve-RepoPath -PathValue $ReportPath -DefaultRelativePath ""
$roadmapFullPath = Resolve-RepoPath -PathValue $RoadmapPath -DefaultRelativePath "docs/PRODUCT_COMPLETION_ROADMAP.md"
$releaseDecisionFullPath = Resolve-RepoPath -PathValue $ReleaseDecisionPath -DefaultRelativePath "docs/release-decision.md"
$stagingValidator = Resolve-RepoPath -PathValue "" -DefaultRelativePath "scripts/validate-staging-smoke-report.ps1"

& $stagingValidator -ReportPath $reportFullPath -RequireAllPassed | Out-Host

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

if ($foundBlockers.Count -gt 0) {
    $payload = @{
        status = "blocked"
        reportPath = $reportFullPath
        blockers = $foundBlockers
    } | ConvertTo-Json -Compress

    throw "Production readiness blocked: $payload"
}

$summary = @{
    status = "production-ready"
    reportPath = $reportFullPath
    roadmapPath = $roadmapFullPath
    releaseDecisionPath = $releaseDecisionFullPath
}

Write-Output ("production readiness valid " + ($summary | ConvertTo-Json -Compress))
