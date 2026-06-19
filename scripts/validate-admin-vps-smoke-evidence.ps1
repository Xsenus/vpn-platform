param(
    [Parameter(Mandatory = $true)]
    [string]$PreflightReportPath,

    [Parameter(Mandatory = $true)]
    [string]$SmokeReportPath
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

function Resolve-WorkspacePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Read-JsonFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Admin VPS smoke evidence file was not found: $Path"
    }

    try {
        return Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "Admin VPS smoke evidence file is not valid JSON: $Path. $($_.Exception.Message)"
    }
}

function Normalize-Url {
    param([AllowEmptyString()][string]$Value)
    return ([string]$Value).Trim().TrimEnd("/")
}

function Assert-Equal {
    param(
        [AllowEmptyString()][string]$Actual,
        [AllowEmptyString()][string]$Expected,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (-not [string]::Equals($Actual, $Expected, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Admin VPS smoke evidence mismatch for $Name. Preflight='$Expected', smoke='$Actual'."
    }
}

$preflightFullPath = Resolve-WorkspacePath $PreflightReportPath
$smokeFullPath = Resolve-WorkspacePath $SmokeReportPath

$preflightValidator = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-preflight-report.ps1"
$smokeValidator = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-report.ps1"

& $preflightValidator -ReportPath $preflightFullPath -RequireReady | Out-Host
& $smokeValidator -ReportPath $smokeFullPath -RequireAllPassed | Out-Host

$preflight = Read-JsonFile -Path $preflightFullPath
$smoke = Read-JsonFile -Path $smokeFullPath

Assert-Equal -Actual (Normalize-Url $smoke.apiBaseUrl) -Expected (Normalize-Url $preflight.apiBaseUrl) -Name "apiBaseUrl"
Assert-Equal -Actual (Normalize-Url $smoke.adminWebUrl) -Expected (Normalize-Url $preflight.adminWebUrl) -Name "adminWebUrl"
Assert-Equal -Actual ([string]$smoke.environmentName) -Expected ([string]$preflight.environmentName) -Name "environmentName"
Assert-Equal -Actual ([string]$smoke.operator) -Expected ([string]$preflight.operator) -Name "operator"

$preflightSmokeReportPath = Resolve-WorkspacePath ([string]$preflight.smokeReportPath)
if (-not [string]::Equals($preflightSmokeReportPath, $smokeFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Admin VPS smoke evidence mismatch for smokeReportPath. Preflight='$preflightSmokeReportPath', smoke='$smokeFullPath'."
}

$preflightReleaseId = ([string]$preflight.releaseId).Trim()
$smokeReleaseId = ([string]$smoke.releaseId).Trim()
$releaseIdsDiffer = -not [string]::Equals($preflightReleaseId, $smokeReleaseId, [System.StringComparison]::Ordinal)
if (-not [string]::IsNullOrWhiteSpace($preflightReleaseId) -and $releaseIdsDiffer) {
    throw "Admin VPS smoke evidence mismatch for releaseId. Preflight='$preflightReleaseId', smoke='$($smoke.releaseId)'."
}

$generatedAt = [DateTimeOffset]::MinValue
$startedAt = [DateTimeOffset]::MinValue
$completedAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$preflight.generatedAt, [ref]$generatedAt)) {
    throw "Admin VPS smoke evidence preflight generatedAt is not a valid DateTimeOffset."
}

if (-not [DateTimeOffset]::TryParse([string]$smoke.startedAt, [ref]$startedAt)) {
    throw "Admin VPS smoke evidence smoke startedAt is not a valid DateTimeOffset."
}

if (-not [DateTimeOffset]::TryParse([string]$smoke.completedAt, [ref]$completedAt)) {
    throw "Admin VPS smoke evidence smoke completedAt is not a valid DateTimeOffset."
}

if ($generatedAt -gt $completedAt) {
    throw "Admin VPS smoke evidence preflight generatedAt must not be after smoke completedAt."
}

$summary = [ordered]@{
    environmentName = $smoke.environmentName
    releaseId = $smoke.releaseId
    apiBaseUrl = (Normalize-Url $smoke.apiBaseUrl)
    adminWebUrl = (Normalize-Url $smoke.adminWebUrl)
    sections = @($smoke.sections).Count
    preflightReady = $preflight.readyForLiveSmoke
    smokeReportPath = $smokeFullPath
}

Write-Host "admin vps smoke evidence valid $($summary | ConvertTo-Json -Compress)"
