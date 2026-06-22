param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$ApiBaseUrl,

    [Parameter(Mandatory = $true)]
    [string]$AdminWebUrl,

    [string]$AdminEmail = $(if ($env:ADMIN_VPS_SMOKE_ADMIN_EMAIL) { $env:ADMIN_VPS_SMOKE_ADMIN_EMAIL } else { "admin@example.test" }),
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
        return "manual-admin-vps-smoke"
    }

    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $latest = @($releases | Where-Object { $_.isActive } | Sort-Object -Property releasedAt -Descending | Select-Object -First 1)
    if ($latest.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$latest[0].releaseId)) {
        return "manual-admin-vps-smoke"
    }

    return [string]$latest[0].releaseId
}

Assert-HttpUrl -Value $ApiBaseUrl -Name "ApiBaseUrl"
Assert-HttpUrl -Value $AdminWebUrl -Name "AdminWebUrl"

if ([string]::IsNullOrWhiteSpace($AdminEmail) -or -not $AdminEmail.Contains("@")) {
    throw "AdminEmail must contain an email address."
}

$templatePath = Resolve-RepoPath "docs/admin-vps-smoke-report.template.json"
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

$report.reportId = "admin-vps-smoke-" + $now.ToString("yyyyMMdd-HHmmss")
$report.environmentName = $EnvironmentName.Trim()
$report.apiBaseUrl = $ApiBaseUrl.TrimEnd("/")
$report.adminWebUrl = $AdminWebUrl.TrimEnd("/")
$report.adminEmail = $AdminEmail.Trim()
$report.smokeReportPath = $fullOutputPath
$report.startedAt = $now.ToString("o")
$report.completedAt = $now.ToString("o")
$report.releaseId = $releaseValue
$report.operator = $operatorValue
$report.notes = "Generated safely. Replace blocked sections with real VPS admin evidence only after browser smoke. Do not include credentials, auth headers, cookies, keys or provider secrets."
$report.accountBootstrapChecked = $false
$report.adminLoginPassed = $false
$report.noJsErrors = $false
$report.noUnauthorizedAfterLogin = $false

foreach ($section in $report.sections) {
    $section.status = "blocked"
    $section.httpStatus = 0
    $section.loaded = $false
    $section.evidence = "TODO: open '$($section.id)' on VPS admin and add sanitized evidence without secrets."
}

$json = $report | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText(
    $fullOutputPath,
    $json,
    [System.Text.UTF8Encoding]::new($false))

$validator = Resolve-RepoPath "scripts/validate-admin-vps-smoke-report.ps1"
& $validator -ReportPath $fullOutputPath | Out-Host

Write-Output "admin vps smoke report generated $fullOutputPath"
