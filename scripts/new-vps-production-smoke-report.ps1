param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$ApiBaseUrl,

    [Parameter(Mandatory = $true)]
    [string]$PublicWebUrl,

    [Parameter(Mandatory = $true)]
    [string]$CabinetWebUrl,

    [Parameter(Mandatory = $true)]
    [string]$AdminWebUrl,

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
        return "manual-vps-production-smoke"
    }

    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $latest = @($releases | Where-Object { $_.isActive } | Sort-Object -Property releasedAt -Descending | Select-Object -First 1)
    if ($latest.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$latest[0].releaseId)) {
        return "manual-vps-production-smoke"
    }

    return [string]$latest[0].releaseId
}

Assert-HttpUrl -Value $ApiBaseUrl -Name "ApiBaseUrl"
Assert-HttpUrl -Value $PublicWebUrl -Name "PublicWebUrl"
Assert-HttpUrl -Value $CabinetWebUrl -Name "CabinetWebUrl"
Assert-HttpUrl -Value $AdminWebUrl -Name "AdminWebUrl"

$templatePath = Resolve-RepoPath "docs/vps-production-smoke-report.template.json"
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

$report.reportId = "vps-production-smoke-" + $now.ToString("yyyyMMdd-HHmmss")
$report.environmentName = $EnvironmentName.Trim()
$report.apiBaseUrl = $ApiBaseUrl.TrimEnd("/")
$report.publicWebUrl = $PublicWebUrl.TrimEnd("/")
$report.cabinetWebUrl = $CabinetWebUrl.TrimEnd("/")
$report.adminWebUrl = $AdminWebUrl.TrimEnd("/")
$report.startedAt = $now.ToString("o")
$report.completedAt = $now.ToString("o")
$report.releaseId = $releaseValue
$report.operator = $operatorValue
$report.notes = "Generated safely. Replace blocked steps with real VPS or staging smoke evidence only after the flow is executed. Do not include credentials, auth headers, cookies, provider secrets, raw webhook payloads or full VPN links."
$report.liveHealthPassed = $false
$report.readyHealthPassed = $false
$report.adminLoginPassed = $false
$report.checkoutCreated = $false
$report.paymentInitialized = $false
$report.paymentConfirmed = $false
$report.subscriptionActivated = $false
$report.vpnAccessIssued = $false
$report.latestReleaseMatched = $false
$report.noJsErrors = $false
$report.noSecretsInEvidence = $false

foreach ($step in $report.steps) {
    $step.status = "blocked"
    $step.httpStatus = 0
    $step.evidence = "TODO: run '$($step.id)' on real VPS/staging and add sanitized evidence without secrets."
}

$json = $report | ConvertTo-Json -Depth 8
Set-Content -LiteralPath $fullOutputPath -Value $json -Encoding UTF8

$validator = Resolve-RepoPath "scripts/validate-vps-production-smoke-report.ps1"
& $validator -ReportPath $fullOutputPath | Out-Host

Write-Output "vps production smoke report generated $fullOutputPath"
