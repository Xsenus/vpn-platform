param(
    [string]$ApiBaseUrl = $env:ADMIN_VPS_SMOKE_API_BASE_URL,
    [string]$AdminWebUrl = $env:ADMIN_VPS_SMOKE_ADMIN_WEB_URL,
    [string]$AdminEmail = $env:ADMIN_VPS_SMOKE_ADMIN_EMAIL,
    [string]$SmokeReportPath = "tmp/admin-vps-smoke-report.json",
    [string]$PreflightReportPath = "tmp/admin-vps-smoke-preflight-report.json",
    [string]$EnvironmentName = $(if ($env:ADMIN_VPS_SMOKE_ENVIRONMENT) { $env:ADMIN_VPS_SMOKE_ENVIRONMENT } else { "staging" }),
    [string]$Operator = $env:ADMIN_VPS_SMOKE_OPERATOR,
    [string]$ReleaseId = $env:ADMIN_VPS_SMOKE_RELEASE_ID,
    [string]$FrontendPath = "frontend",
    [string]$MaxEvidenceChainMinutes = $(if ($env:ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES) { $env:ADMIN_VPS_SMOKE_MAX_EVIDENCE_CHAIN_MINUTES } else { "120" }),
    [switch]$AccountBootstrapChecked
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$preflightScript = Join-Path $repoRoot "scripts/admin-vps-smoke-preflight.ps1"
$browserSmokeScript = Join-Path $repoRoot "scripts/admin-vps-browser-smoke.ps1"
$reportValidatorScript = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-report.ps1"
$preflightValidatorScript = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-preflight-report.ps1"
$evidenceValidatorScript = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-evidence.ps1"

function Get-LatestReleaseId {
    $releasesPath = Join-Path $repoRoot "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    if (-not (Test-Path -LiteralPath $releasesPath -PathType Leaf)) {
        return "manual-admin-vps-smoke-flow"
    }

    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $latest = @($releases | Where-Object { $_.isActive } | Sort-Object -Property { [System.DateTimeOffset]::Parse([string]$_.releasedAt, [System.Globalization.CultureInfo]::InvariantCulture) } -Descending | Select-Object -First 1)
    if ($latest.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$latest[0].releaseId)) {
        return "manual-admin-vps-smoke-flow"
    }

    return [string]$latest[0].releaseId
}

function Assert-KnownReleaseId {
    param([Parameter(Mandatory = $true)][string]$Value)

    $releasesPath = Join-Path $repoRoot "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    if (-not (Test-Path -LiteralPath $releasesPath -PathType Leaf)) {
        return
    }

    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $matchedRelease = @($releases | Where-Object { [string]$_.releaseId -eq $Value } | Select-Object -First 1)
    if ($matchedRelease.Count -eq 0) {
        throw "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json."
    }
}

function Convert-MaxEvidenceChainMinutes {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "MaxEvidenceChainMinutes must be an integer."
    }

    $parsed = 0
    if (-not [int]::TryParse($Value.Trim(), [System.Globalization.NumberStyles]::Integer, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$parsed)) {
        throw "MaxEvidenceChainMinutes must be an integer."
    }

    if ($parsed -le 0) {
        throw "MaxEvidenceChainMinutes must be greater than 0."
    }

    if ($parsed -gt 1440) {
        throw "MaxEvidenceChainMinutes must be less than or equal to 1440."
    }

    return $parsed
}

function Assert-HttpUrl {
    param(
        [AllowEmptyString()][string]$Value,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $parsed = $null
    $isInvalid = [string]::IsNullOrWhiteSpace($Value) -or -not [Uri]::TryCreate($Value.Trim(), [UriKind]::Absolute, [ref]$parsed) -or ($parsed.Scheme -ne "http" -and $parsed.Scheme -ne "https")
    if ($isInvalid) {
        throw "$Name must be an absolute http or https URL."
    }
}

function Assert-AdminEmail {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value) -or -not $Value.Trim().Contains("@")) {
        throw "AdminEmail must contain an email address."
    }
}

function Get-ReportPathFullName {
    param(
        [AllowEmptyString()][string]$Path,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "$Name must not be empty."
    }

    $candidate = if ([System.IO.Path]::IsPathRooted($Path)) {
        $Path
    }
    else {
        Join-Path $repoRoot $Path
    }

    return [System.IO.Path]::GetFullPath($candidate)
}

function Assert-DistinctReportPaths {
    param([Parameter(Mandatory = $true)][object[]]$Reports)

    $seen = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($report in $Reports) {
        $name = [string]$report.Name
        $fullPath = Get-ReportPathFullName -Path ([string]$report.Path) -Name $name
        if ($seen.ContainsKey($fullPath)) {
            throw "$name must be different from $($seen[$fullPath])."
        }

        $seen.Add($fullPath, $name)
    }
}

function Get-OperatorValue {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return "manual-operator"
    }

    return $Value.Trim()
}

function Get-EnvironmentNameValue {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return "staging"
    }

    return $Value.Trim()
}

foreach ($requiredScript in @($preflightScript, $browserSmokeScript, $reportValidatorScript, $preflightValidatorScript, $evidenceValidatorScript)) {
    if (-not (Test-Path -LiteralPath $requiredScript -PathType Leaf)) {
        throw "Required admin VPS smoke script was not found: $requiredScript"
    }
}

$maxEvidenceChainMinutesValue = Convert-MaxEvidenceChainMinutes -Value $MaxEvidenceChainMinutes
Assert-HttpUrl -Value $ApiBaseUrl -Name "ApiBaseUrl"
Assert-HttpUrl -Value $AdminWebUrl -Name "AdminWebUrl"
Assert-AdminEmail -Value $AdminEmail
Assert-DistinctReportPaths -Reports @(
    @{ Name = "SmokeReportPath"; Path = $SmokeReportPath },
    @{ Name = "PreflightReportPath"; Path = $PreflightReportPath }
)

$releaseValue = if ([string]::IsNullOrWhiteSpace($ReleaseId)) { Get-LatestReleaseId } else { $ReleaseId.Trim() }
Assert-KnownReleaseId -Value $releaseValue
$operatorValue = Get-OperatorValue -Value $Operator
$environmentNameValue = Get-EnvironmentNameValue -Value $EnvironmentName

Write-Host "Admin VPS smoke flow is ready to run."
Write-Host "Environment: $environmentNameValue"
Write-Host "API base URL: $ApiBaseUrl"
Write-Host "Admin web URL: $AdminWebUrl"
Write-Host "Admin email: $AdminEmail"
Write-Host "Operator: $operatorValue"
Write-Host "Password: [hidden]"
Write-Host "Smoke report path: $SmokeReportPath"
Write-Host "Preflight report path: $PreflightReportPath"
Write-Host "Release id: $releaseValue"
Write-Host "Max evidence chain minutes: $maxEvidenceChainMinutesValue"
Write-Host "Account bootstrap checked: $AccountBootstrapChecked"

& $preflightScript `
    -ApiBaseUrl $ApiBaseUrl `
    -AdminWebUrl $AdminWebUrl `
    -AdminEmail $AdminEmail `
    -SmokeReportPath $SmokeReportPath `
    -PreflightReportPath $PreflightReportPath `
    -EnvironmentName $environmentNameValue `
    -Operator $operatorValue `
    -ReleaseId $releaseValue `
    -FrontendPath $FrontendPath `
    -RequirePassword

& $browserSmokeScript `
    -ApiBaseUrl $ApiBaseUrl `
    -AdminWebUrl $AdminWebUrl `
    -AdminEmail $AdminEmail `
    -OutputPath $SmokeReportPath `
    -EnvironmentName $environmentNameValue `
    -Operator $operatorValue `
    -ReleaseId $releaseValue `
    -FrontendPath $FrontendPath `
    -AccountBootstrapChecked:$AccountBootstrapChecked `
    -RequireAllPassed

& $evidenceValidatorScript `
    -PreflightReportPath $PreflightReportPath `
    -SmokeReportPath $SmokeReportPath `
    -MaxEvidenceChainMinutes $maxEvidenceChainMinutesValue

Write-Host "Admin VPS smoke flow completed."
Write-Host "Validated preflight report: $PreflightReportPath"
Write-Host "Validated smoke report: $SmokeReportPath"
