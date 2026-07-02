param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$ApiBaseUrl,

    [Parameter(Mandatory = $true)]
    [string]$AdminWebUrl,

    [Parameter(Mandatory = $true)]
    [string]$X3uiPanelUrl,

    [string]$EnvironmentName = "staging",
    [string]$Operator = "",
    [string]$ReleaseId = "",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $repoRoot = Split-Path -Parent $PSScriptRoot
    return Join-Path $repoRoot $RelativePath
}

function Assert-HttpUrl {
    param(
        [string]$Value,
        [string]$Name
    )

    $parsed = $null
    $isInvalid = [string]::IsNullOrWhiteSpace($Value) -or -not [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$parsed) -or ($parsed.Scheme -ne "http" -and $parsed.Scheme -ne "https")
    if ($isInvalid) {
        throw "$Name must be an absolute http or https URL."
    }
}

function Get-LatestReleaseId {
    $releasesPath = Resolve-RepoPath "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    if (-not (Test-Path -LiteralPath $releasesPath)) {
        return "manual-vpn-live-smoke"
    }

    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $latest = @($releases | Where-Object { $_.isActive } | Sort-Object -Property { [System.DateTimeOffset]::Parse([string]$_.releasedAt, [System.Globalization.CultureInfo]::InvariantCulture) } -Descending | Select-Object -First 1)
    if ($latest.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$latest[0].releaseId)) {
        return "manual-vpn-live-smoke"
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

Assert-HttpUrl -Value $ApiBaseUrl -Name "ApiBaseUrl"
Assert-HttpUrl -Value $AdminWebUrl -Name "AdminWebUrl"
Assert-HttpUrl -Value $X3uiPanelUrl -Name "X3uiPanelUrl"

$templatePath = Resolve-RepoPath "docs/vpn-live-smoke-report.template.json"
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

$report.reportId = "vpn-live-smoke-" + $now.ToString("yyyyMMdd-HHmmss")
$report.environmentName = $EnvironmentName.Trim()
$report.apiBaseUrl = $ApiBaseUrl.TrimEnd("/")
$report.adminWebUrl = $AdminWebUrl.TrimEnd("/")
$report.x3uiPanelUrl = $X3uiPanelUrl.TrimEnd("/")
$report.smokeReportPath = $fullOutputPath
$report.startedAt = $now.ToString("o")
$report.completedAt = $now.ToString("o")
$report.releaseId = $releaseValue
$report.operator = $operatorValue
$report.notes = "Generated safely. Replace blocked checks with real 3x-ui/VPN evidence only after production-like smoke. Do not include credentials, auth headers, cookies, keys or full VPN URIs."
$report.panelConnected = $false
$report.inboundSynced = $false
$report.nodeReady = $false
$report.productionProvisioningEnabled = $false
$report.noSandboxFallback = $false
$report.failClosedChecked = $false

foreach ($check in $report.checks) {
    $check.status = "blocked"
    $check.evidence = "TODO: run live VPN smoke step '$($check.id)' and add sanitized evidence without secrets."
}

$json = $report | ConvertTo-Json -Depth 8
Set-Content -LiteralPath $fullOutputPath -Value $json -Encoding UTF8

$validator = Resolve-RepoPath "scripts/validate-vpn-live-smoke-report.ps1"
& $validator -ReportPath $fullOutputPath | Out-Host

Write-Output "vpn live smoke report generated $fullOutputPath"
