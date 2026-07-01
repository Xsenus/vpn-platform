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
    [switch]$RequirePassword,
    [switch]$RequireRemoteReleaseMatch
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$checks = [System.Collections.Generic.List[object]]::new()

function Add-Check {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][bool]$Passed,
        [Parameter(Mandatory = $true)][string]$Message
    )

    $script:checks.Add([ordered]@{
        name = $Name
        passed = $Passed
        message = $Message
    })
}

function Test-HttpUrl {
    param([AllowEmptyString()][string]$Value)

    $parsed = $null
    return -not [string]::IsNullOrWhiteSpace($Value) `
        -and [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$parsed) `
        -and ($parsed.Scheme -eq "http" -or $parsed.Scheme -eq "https")
}

function Resolve-WorkspacePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Get-WorkspacePathValue {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return $Value.Trim()
}

function Get-LatestReleaseId {
    $releasesPath = Join-Path $repoRoot "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    if (-not (Test-Path -LiteralPath $releasesPath -PathType Leaf)) {
        return "manual-admin-vps-smoke-preflight"
    }

    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $latest = @($releases | Where-Object { $_.isActive } | Sort-Object -Property { [System.DateTimeOffset]::Parse([string]$_.releasedAt, [System.Globalization.CultureInfo]::InvariantCulture) } -Descending | Select-Object -First 1)
    if ($latest.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$latest[0].releaseId)) {
        return "manual-admin-vps-smoke-preflight"
    }

    return [string]$latest[0].releaseId
}

function Assert-KnownReleaseId {
    param([Parameter(Mandatory = $true)][string]$Value)

    $releasesPath = Join-Path $repoRoot "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    if (-not (Test-Path -LiteralPath $releasesPath -PathType Leaf)) {
        throw "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json."
    }

    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $matchedRelease = @($releases | Where-Object { [string]$_.releaseId -eq $Value } | Select-Object -First 1)
    if ($matchedRelease.Count -eq 0) {
        throw "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json."
    }
}

function Get-RemoteAdminAccessToken {
    param(
        [Parameter(Mandatory = $true)][string]$BaseUrl,
        [Parameter(Mandatory = $true)][string]$Email
    )

    if ([string]::IsNullOrWhiteSpace($Email) -or [string]::IsNullOrWhiteSpace($env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD)) {
        return ""
    }

    try {
        $loginUri = "$($BaseUrl.TrimEnd('/'))/api/auth/login"
        $loginBody = @{
            email = $Email.Trim()
            password = $env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD
        } | ConvertTo-Json -Depth 3
        $response = Invoke-RestMethod -Method Post -Uri $loginUri -Body $loginBody -ContentType "application/json" -TimeoutSec 15
        $accessToken = [string]$response.accessToken
        if ([string]::IsNullOrWhiteSpace($accessToken)) {
            return ""
        }

        return $accessToken
    }
    catch {
        return ""
    }
}

function Get-RemoteLatestReleaseId {
    param(
        [Parameter(Mandatory = $true)][string]$BaseUrl,
        [Parameter(Mandatory = $true)][string]$Email
    )

    try {
        $accessToken = Get-RemoteAdminAccessToken -BaseUrl $BaseUrl -Email $Email
        if ([string]::IsNullOrWhiteSpace($accessToken)) {
            return ""
        }

        $latestUri = "$($BaseUrl.TrimEnd('/'))/api/app-version/latest"
        $response = Invoke-RestMethod -Method Get -Uri $latestUri -Headers @{ Authorization = "Bearer $accessToken" } -TimeoutSec 15
        $remoteReleaseId = [string]$response.latestRelease.releaseId
        if ([string]::IsNullOrWhiteSpace($remoteReleaseId)) {
            return ""
        }

        return $remoteReleaseId
    }
    catch {
        return ""
    }
}

$frontendPathValue = Get-WorkspacePathValue -Value $FrontendPath
$frontendFullPath = Resolve-WorkspacePath $frontendPathValue
$smokeReportFullPath = Resolve-WorkspacePath $SmokeReportPath
$preflightReportFullPath = Resolve-WorkspacePath $PreflightReportPath
$packageJsonPath = Join-Path $frontendFullPath "package.json"
$runnerPath = Join-Path $repoRoot "scripts/admin-vps-browser-smoke.ps1"
$validatorPath = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-report.ps1"
$preflightValidatorPath = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-preflight-report.ps1"
$passwordPresent = -not [string]::IsNullOrWhiteSpace($env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD)

Add-Check "api-base-url" (Test-HttpUrl $ApiBaseUrl) "ADMIN_VPS_SMOKE_API_BASE_URL must be an absolute http/https URL."
Add-Check "admin-web-url" (Test-HttpUrl $AdminWebUrl) "ADMIN_VPS_SMOKE_ADMIN_WEB_URL must be an absolute http/https URL."
Add-Check "admin-email" (-not [string]::IsNullOrWhiteSpace($AdminEmail) -and $AdminEmail.Contains("@")) "ADMIN_VPS_SMOKE_ADMIN_EMAIL must be set."
Add-Check "password-env-present" $passwordPresent "ADMIN_VPS_SMOKE_ADMIN_PASSWORD must be set in the process environment and is never printed."
Add-Check "frontend-directory" (Test-Path -LiteralPath $frontendFullPath -PathType Container) "Frontend directory must exist."
Add-Check "package-command" ((Test-Path -LiteralPath $packageJsonPath -PathType Leaf) -and ((Get-Content -Raw $packageJsonPath).Contains("e2e:admin-vps-smoke"))) "frontend/package.json must expose e2e:admin-vps-smoke."
Add-Check "browser-runner" (Test-Path -LiteralPath $runnerPath -PathType Leaf) "scripts/admin-vps-browser-smoke.ps1 must exist."
Add-Check "report-validator" (Test-Path -LiteralPath $validatorPath -PathType Leaf) "scripts/validate-admin-vps-smoke-report.ps1 must exist."
Add-Check "preflight-validator" (Test-Path -LiteralPath $preflightValidatorPath -PathType Leaf) "scripts/validate-admin-vps-smoke-preflight-report.ps1 must exist."

$ready = $checks | Where-Object { -not $_.passed } | Select-Object -First 1
$generatedAt = (Get-Date).ToUniversalTime()
$releaseValue = if ([string]::IsNullOrWhiteSpace($ReleaseId)) { Get-LatestReleaseId } else { $ReleaseId.Trim() }
if (-not [string]::IsNullOrWhiteSpace($ReleaseId)) {
    Assert-KnownReleaseId -Value $releaseValue
}
$remoteReleaseId = ""
$remoteReleaseStatus = "not-required"
$remoteReleaseMessage = "Remote release check was not required for this preflight run."
if ($RequireRemoteReleaseMatch -and (Test-HttpUrl $ApiBaseUrl)) {
    $remoteReleaseId = Get-RemoteLatestReleaseId -BaseUrl $ApiBaseUrl -Email $AdminEmail
}

$remoteReleaseMatches = -not $RequireRemoteReleaseMatch -or ((-not [string]::IsNullOrWhiteSpace($remoteReleaseId)) -and $remoteReleaseId -eq $releaseValue)
if ($RequireRemoteReleaseMatch) {
    if ([string]::IsNullOrWhiteSpace($remoteReleaseId)) {
        $remoteReleaseStatus = "unavailable"
        $remoteReleaseMessage = "Remote latest release could not be read with the admin account. Check admin credentials, API reachability and deployment health before browser smoke."
    }
    elseif ($remoteReleaseMatches) {
        $remoteReleaseStatus = "matched"
        $remoteReleaseMessage = "Remote latest release matches the local smoke release."
    }
    else {
        $remoteReleaseStatus = "mismatch"
        $remoteReleaseMessage = "Remote latest release differs from the local smoke release. Deploy the latest local release before running browser smoke."
    }
}

Add-Check "remote-latest-release" $remoteReleaseMatches "Remote /api/app-version/latest releaseId must match the smoke ReleaseId before live browser smoke."

$failedChecks = @($checks | Where-Object { -not $_.passed } | ForEach-Object { [string]$_.name })
$checkCount = $checks.Count
$failedCheckCount = $failedChecks.Count
$passedCheckCount = $checkCount - $failedCheckCount
$readyForLiveSmoke = $failedCheckCount -eq 0

$report = [ordered]@{
    reportId = "admin-vps-smoke-preflight-" + $generatedAt.ToString("yyyyMMdd-HHmmss")
    generatedAt = $generatedAt.ToString("O")
    environmentName = $EnvironmentName
    operator = $Operator
    releaseId = $releaseValue
    remoteReleaseId = $remoteReleaseId
    remoteReleaseCheckRequired = [bool]$RequireRemoteReleaseMatch
    remoteReleaseMatched = [bool]$remoteReleaseMatches
    remoteReleaseStatus = $remoteReleaseStatus
    remoteReleaseMessage = $remoteReleaseMessage
    apiBaseUrl = $ApiBaseUrl
    adminWebUrl = $AdminWebUrl
    adminEmail = $AdminEmail
    smokeReportPath = $smokeReportFullPath
    preflightReportPath = $preflightReportFullPath
    passwordEnvPresent = $passwordPresent
    readyForLiveSmoke = $readyForLiveSmoke
    checkCount = $checkCount
    passedCheckCount = $passedCheckCount
    failedCheckCount = $failedCheckCount
    failedChecks = @($failedChecks)
    checks = @($checks)
}

$preflightDirectory = Split-Path -Parent $preflightReportFullPath
if (-not (Test-Path -LiteralPath $preflightDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $preflightDirectory -Force | Out-Null
}

[System.IO.File]::WriteAllText(
    $preflightReportFullPath,
    ($report | ConvertTo-Json -Depth 8),
    [System.Text.UTF8Encoding]::new($false))

Write-Host "Admin VPS smoke preflight completed."
Write-Host "Preflight report id: $($report.reportId)"
Write-Host "API base URL: $ApiBaseUrl"
Write-Host "Admin web URL: $AdminWebUrl"
Write-Host "Admin email: $AdminEmail"
Write-Host "Password env: $(if ($passwordPresent) { 'present [hidden]' } else { 'missing' })"
Write-Host "Remote release status: $remoteReleaseStatus"
Write-Host "Remote release message: $remoteReleaseMessage"
Write-Host "Remote release expected: $releaseValue"
Write-Host "Remote release actual: $(if ([string]::IsNullOrWhiteSpace($remoteReleaseId)) { '[none]' } else { $remoteReleaseId })"
Write-Host "Check count: $checkCount"
Write-Host "Passed checks: $passedCheckCount/$checkCount"
Write-Host "Failed check count: $failedCheckCount"
Write-Host "Failed checks: $(if ($failedChecks.Count -eq 0) { '[none]' } else { $failedChecks -join ', ' })"
Write-Host "Smoke report path: $smokeReportFullPath"
Write-Host "Preflight report path: $preflightReportFullPath"
Write-Host "Ready for live smoke: $readyForLiveSmoke"

& $preflightValidatorPath -ReportPath $preflightReportFullPath -RequireReady

if (-not $readyForLiveSmoke) {
    throw "Admin VPS smoke preflight failed. Fix the failed checks before running live smoke."
}

if ($RequirePassword -and -not $passwordPresent) {
    throw "ADMIN_VPS_SMOKE_ADMIN_PASSWORD is required for live smoke."
}
