param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$ApiBaseUrl,

    [string]$PublicWebUrl = "",
    [string]$CabinetWebUrl = "",
    [string]$AdminWebUrl = "",
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
        [string]$Name,
        [bool]$Required
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        if ($Required) {
            throw "$Name is required."
        }

        return
    }

    $parsed = $null
    if (-not [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$parsed) -or ($parsed.Scheme -ne "http" -and $parsed.Scheme -ne "https")) {
        throw "$Name must be an absolute http or https URL."
    }
}

function Get-LatestReleaseId {
    $releasesPath = Resolve-RepoPath "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    if (-not (Test-Path -LiteralPath $releasesPath)) {
        return "manual-staging-smoke"
    }

    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $latest = @($releases | Where-Object { $_.isActive } | Sort-Object -Property { [System.DateTimeOffset]::Parse([string]$_.releasedAt, [System.Globalization.CultureInfo]::InvariantCulture) } -Descending | Select-Object -First 1)
    if ($latest.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$latest[0].releaseId)) {
        return "manual-staging-smoke"
    }

    return [string]$latest[0].releaseId
}

Assert-HttpUrl -Value $ApiBaseUrl -Name "ApiBaseUrl" -Required $true
Assert-HttpUrl -Value $PublicWebUrl -Name "PublicWebUrl" -Required $false
Assert-HttpUrl -Value $CabinetWebUrl -Name "CabinetWebUrl" -Required $false
Assert-HttpUrl -Value $AdminWebUrl -Name "AdminWebUrl" -Required $false

$templatePath = Resolve-RepoPath "docs/staging-smoke-report.template.json"
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

$report.reportId = "staging-smoke-" + $now.ToString("yyyyMMdd-HHmmss")
$report.environmentName = $EnvironmentName.Trim()
$report.apiBaseUrl = $ApiBaseUrl.TrimEnd("/")
$report.publicWebUrl = $PublicWebUrl.Trim()
$report.cabinetWebUrl = $CabinetWebUrl.Trim()
$report.adminWebUrl = $AdminWebUrl.Trim()
$report.startedAt = $now.ToString("o")
$report.completedAt = $now.ToString("o")
$report.releaseId = $releaseValue
$report.operator = $operatorValue
$report.notes = "Generated safely. Replace blocked checks with real evidence only after staging/VPS smoke. Do not include credentials, auth headers, cookies, keys or provider secrets."

foreach ($check in $report.checks) {
    $check.status = "blocked"
    $check.evidence = "TODO: run staging/VPS smoke and add sanitized evidence for '$($check.id)'."
}

$json = $report | ConvertTo-Json -Depth 8
Set-Content -LiteralPath $fullOutputPath -Value $json -Encoding UTF8

$validator = Resolve-RepoPath "scripts/validate-staging-smoke-report.ps1"
& $validator -ReportPath $fullOutputPath | Out-Host

Write-Output "staging smoke report generated $fullOutputPath"
