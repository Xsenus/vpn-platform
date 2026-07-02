param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [string]$EnvironmentName = "staging",
    [string]$Operator = "",
    [string]$ReleaseId = "",
    [ValidateSet("sandbox", "live")]
    [string]$Mode = "sandbox",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $repoRoot = Split-Path -Parent $PSScriptRoot
    return Join-Path $repoRoot $RelativePath
}

function Get-LatestReleaseId {
    $releasesPath = Resolve-RepoPath "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    if (-not (Test-Path -LiteralPath $releasesPath)) {
        return "manual-payment-provider-smoke"
    }

    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $latest = @($releases | Where-Object { $_.isActive } | Sort-Object -Property { [System.DateTimeOffset]::Parse([string]$_.releasedAt, [System.Globalization.CultureInfo]::InvariantCulture) } -Descending | Select-Object -First 1)
    if ($latest.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$latest[0].releaseId)) {
        return "manual-payment-provider-smoke"
    }

    return [string]$latest[0].releaseId
}

function Assert-KnownReleaseId {
    param([Parameter(Mandatory = $true)][string]$Value)

    $releasesPath = Resolve-RepoPath "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    if (-not (Test-Path -LiteralPath $releasesPath)) {
        throw "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json."
    }

    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $matchedRelease = @($releases | Where-Object { [string]$_.releaseId -eq $Value } | Select-Object -First 1)
    if ($matchedRelease.Count -eq 0) {
        throw "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json."
    }
}

$templatePath = Resolve-RepoPath "docs/payment-provider-smoke-report.template.json"
if (-not (Test-Path -LiteralPath $templatePath)) {
    throw "Template was not found: $templatePath"
}

$fullOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
if ((Test-Path -LiteralPath $fullOutputPath) -and -not $Force) {
    throw "Output file already exists. Pass -Force to overwrite: $fullOutputPath"
}

$parent = Split-Path -Parent $fullOutputPath
if (-not [string]::IsNullOrWhiteSpace($parent) -and -not (Test-Path -LiteralPath $parent)) {
    New-Item -ItemType Directory -Path $parent | Out-Null
}

$report = Get-Content -LiteralPath $templatePath -Raw -Encoding UTF8 | ConvertFrom-Json
$now = [DateTimeOffset]::UtcNow
$operatorValue = if ([string]::IsNullOrWhiteSpace($Operator)) {
    if ([string]::IsNullOrWhiteSpace($env:GITHUB_RUN_ID)) { $env:USERNAME } else { "github-run-$($env:GITHUB_RUN_ID)" }
} else {
    $Operator.Trim()
}

if ([string]::IsNullOrWhiteSpace($operatorValue)) {
    $operatorValue = "manual-operator"
}

$releaseValue = if ([string]::IsNullOrWhiteSpace($ReleaseId)) { Get-LatestReleaseId } else { $ReleaseId.Trim() }
if (-not [string]::IsNullOrWhiteSpace($ReleaseId)) {
    Assert-KnownReleaseId -Value $releaseValue
}

$report.reportId = "payment-provider-smoke-" + $now.ToString("yyyyMMdd-HHmmss")
$report.environmentName = $EnvironmentName.Trim()
$report.startedAt = $now.ToString("o")
$report.completedAt = $now.ToString("o")
$report.smokeReportPath = $fullOutputPath
$report.releaseId = $releaseValue
$report.operator = $operatorValue
$report.notes = "Generated safely. Replace blocked providers with real sandbox/live evidence only after external provider smoke. Do not include credentials, auth headers, cookies, keys or provider secrets."

foreach ($provider in $report.providers) {
    $provider.mode = $Mode
    $provider.status = "blocked"
    $provider.accountConfigured = $false
    $provider.checkoutCreated = $false
    $provider.providerConfirmation = $false
    $provider.webhookProcessed = $false
    $provider.subscriptionActivated = $false
    $provider.refundChecked = $false
    $provider.evidence = "TODO: run $Mode smoke for '$($provider.provider)' and add sanitized payment, webhook, order and subscription identifiers."
}

$json = $report | ConvertTo-Json -Depth 8
Set-Content -LiteralPath $fullOutputPath -Value $json -Encoding UTF8

$validator = Resolve-RepoPath "scripts/validate-payment-provider-smoke-report.ps1"
& $validator -ReportPath $fullOutputPath | Out-Host

Write-Output "payment provider smoke report generated $fullOutputPath"
