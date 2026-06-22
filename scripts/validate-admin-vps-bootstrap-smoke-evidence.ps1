param(
    [Parameter(Mandatory = $true)]
    [string]$ReadinessReportPath,

    [Parameter(Mandatory = $true)]
    [string]$BootstrapSmokeReportPath,

    [string]$ExpectedReadinessReportSha256 = "",

    [string]$ExpectedBootstrapSmokeReportSha256 = "",

    [string]$ExpectedPreflightReportSha256 = "",

    [string]$ExpectedSmokeReportSha256 = "",

    [int]$MaxEvidenceChainMinutes = 120
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$readinessValidatorScript = Join-Path $repoRoot "scripts/validate-admin-vps-bootstrap-smoke-readiness-report.ps1"
$bootstrapValidatorScript = Join-Path $repoRoot "scripts/validate-admin-vps-bootstrap-smoke-report.ps1"

if ($MaxEvidenceChainMinutes -le 0) {
    throw "MaxEvidenceChainMinutes must be greater than 0."
}

if ($MaxEvidenceChainMinutes -gt 1440) {
    throw "MaxEvidenceChainMinutes must be less than or equal to 1440."
}

function Resolve-WorkspacePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Normalize-Url {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    return $Value.Trim().TrimEnd("/")
}

function Get-FileSha256 {
    param([Parameter(Mandatory = $true)][string]$Path)

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.IO.File]::ReadAllBytes($Path)
        $hash = $sha256.ComputeHash($bytes)
        return [System.BitConverter]::ToString($hash).Replace("-", "").ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
    }
}

function Assert-ExpectedSha256 {
    param(
        [Parameter(Mandatory = $true)][string]$Actual,
        [AllowEmptyString()][string]$Expected,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ([string]::IsNullOrWhiteSpace($Expected)) {
        return
    }

    $normalizedExpected = $Expected.Trim().ToLowerInvariant()
    if (-not ($normalizedExpected -match "^[0-9a-f]{64}$")) {
        throw "Admin VPS bootstrap smoke evidence expected $Name must be a 64-character SHA256 hex string."
    }

    if (-not [string]::Equals($Actual, $normalizedExpected, [System.StringComparison]::Ordinal)) {
        throw "Admin VPS bootstrap smoke evidence $Name does not match expected SHA256."
    }
}

function Assert-Same {
    param(
        [AllowEmptyString()][string]$Left,
        [AllowEmptyString()][string]$Right,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (-not [string]::Equals($Left, $Right, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Admin VPS bootstrap smoke evidence mismatch for $Name."
    }
}

function Assert-ReportIdPrefix {
    param(
        [AllowEmptyString()][string]$Value,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Prefix,
        [string]$ForbiddenPrefix = ""
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "Admin VPS bootstrap smoke evidence $Name reportId is required."
    }

    if (-not $Value.StartsWith($Prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Admin VPS bootstrap smoke evidence $Name reportId must start with $Prefix."
    }

    if (-not [string]::IsNullOrWhiteSpace($ForbiddenPrefix) `
        -and $Value.StartsWith($ForbiddenPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Admin VPS bootstrap smoke evidence $Name reportId must not use the $ForbiddenPrefix prefix."
    }

    $timestampPattern = "^" + [regex]::Escape($Prefix) + "\d{8}-\d{6}$"
    if ($Value -notmatch $timestampPattern) {
        throw "Admin VPS bootstrap smoke evidence $Name reportId must match $($Prefix)yyyyMMdd-HHmmss."
    }
}

function Assert-ReportIdTimestampMatches {
    param(
        [Parameter(Mandatory = $true)][string]$Value,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Prefix,
        [Parameter(Mandatory = $true)][DateTimeOffset]$ExpectedAt,
        [Parameter(Mandatory = $true)][string]$TimestampField
    )

    $actualTimestamp = $Value.Substring($Prefix.Length)
    $expectedTimestamp = $ExpectedAt.ToUniversalTime().ToString("yyyyMMdd-HHmmss")
    if (-not [string]::Equals($actualTimestamp, $expectedTimestamp, [System.StringComparison]::Ordinal)) {
        throw "Admin VPS bootstrap smoke evidence $Name reportId timestamp must match $TimestampField."
    }
}

$readinessFullPath = Resolve-WorkspacePath $ReadinessReportPath
$bootstrapFullPath = Resolve-WorkspacePath $BootstrapSmokeReportPath

& $readinessValidatorScript -ReportPath $readinessFullPath -RequireReady | Out-Host
& $bootstrapValidatorScript -ReportPath $bootstrapFullPath -RequirePassed | Out-Host

$readiness = Get-Content -LiteralPath $readinessFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
$bootstrap = Get-Content -LiteralPath $bootstrapFullPath -Raw -Encoding UTF8 | ConvertFrom-Json

Assert-Same (Normalize-Url ([string]$readiness.apiBaseUrl)) (Normalize-Url ([string]$bootstrap.apiBaseUrl)) "apiBaseUrl"
Assert-Same (Normalize-Url ([string]$readiness.adminWebUrl)) (Normalize-Url ([string]$bootstrap.adminWebUrl)) "adminWebUrl"
Assert-Same ([string]$readiness.adminEmail) ([string]$bootstrap.adminEmail) "adminEmail"
Assert-Same ([string]$readiness.environmentName) ([string]$bootstrap.environmentName) "environmentName"
Assert-Same ([string]$readiness.operator) ([string]$bootstrap.operator) "operator"
Assert-Same ([string]$readiness.releaseId) ([string]$bootstrap.releaseId) "releaseId"
Assert-Same ([string]$readiness.provider) ([string]$bootstrap.provider) "provider"
Assert-Same (Resolve-WorkspacePath ([string]$readiness.readinessReportPath)) $readinessFullPath "readinessReportPath"
Assert-Same (Resolve-WorkspacePath ([string]$bootstrap.readinessReportPath)) $readinessFullPath "bootstrap readinessReportPath"
Assert-Same (Resolve-WorkspacePath ([string]$bootstrap.bootstrapSmokeReportPath)) $bootstrapFullPath "bootstrap bootstrapSmokeReportPath"
Assert-Same (Resolve-WorkspacePath ([string]$readiness.smokeReportPath)) (Resolve-WorkspacePath ([string]$bootstrap.smokeReportPath)) "smokeReportPath"
Assert-Same (Resolve-WorkspacePath ([string]$readiness.preflightReportPath)) (Resolve-WorkspacePath ([string]$bootstrap.preflightReportPath)) "preflightReportPath"
Assert-Same (Resolve-WorkspacePath ([string]$readiness.bootstrapSmokeReportPath)) $bootstrapFullPath "bootstrapSmokeReportPath"

if ($readiness.localSqlite -isnot [bool] -or $bootstrap.localSqlite -isnot [bool] -or $readiness.localSqlite -ne $bootstrap.localSqlite) {
    throw "Admin VPS bootstrap smoke evidence mismatch for localSqlite."
}

if (-not $readiness.localSqlite -and -not $readiness.confirmBootstrapReset) {
    throw "Admin VPS bootstrap smoke evidence requires confirmBootstrapReset for non-local database."
}

if (-not $readiness.localSqlite -and -not $bootstrap.bootstrapResetConfirmed) {
    throw "Admin VPS bootstrap smoke evidence requires bootstrapResetConfirmed for non-local database."
}

if (-not $readiness.passwordEnvPresent -or -not $bootstrap.passwordEnvPresent) {
    throw "Admin VPS bootstrap smoke evidence requires password env confirmation in both reports."
}

if (-not $readiness.readyForBootstrapSmoke) {
    throw "Admin VPS bootstrap smoke evidence requires readyForBootstrapSmoke."
}

if ([string]$bootstrap.status -ne "passed") {
    throw "Admin VPS bootstrap smoke evidence requires passed bootstrap smoke status."
}

$readinessGeneratedAt = [DateTimeOffset]::Parse([string]$readiness.generatedAt)
$bootstrapGeneratedAt = [DateTimeOffset]::Parse([string]$bootstrap.generatedAt)
$bootstrapCompletedAt = [DateTimeOffset]::Parse([string]$bootstrap.completedAt)

if ($bootstrapGeneratedAt -lt $readinessGeneratedAt) {
    throw "Admin VPS bootstrap smoke evidence bootstrap report must be generated after readiness report."
}

if ($bootstrapCompletedAt -lt $bootstrapGeneratedAt) {
    throw "Admin VPS bootstrap smoke evidence bootstrap completedAt must not be earlier than generatedAt."
}

$sectionsContractPath = Resolve-WorkspacePath "docs/admin-vps-smoke-sections.json"
$preflightFullPath = Resolve-WorkspacePath ([string]$bootstrap.preflightReportPath)
$smokeFullPath = Resolve-WorkspacePath ([string]$bootstrap.smokeReportPath)
$preflight = Get-Content -LiteralPath $preflightFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
$smoke = Get-Content -LiteralPath $smokeFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
$preflightGeneratedAt = [DateTimeOffset]::Parse([string]$preflight.generatedAt)
$smokeStartedAt = [DateTimeOffset]::Parse([string]$smoke.startedAt)
$smokeCompletedAt = [DateTimeOffset]::Parse([string]$smoke.completedAt)

$readinessReportId = ([string]$readiness.reportId).Trim()
$bootstrapSmokeReportId = ([string]$bootstrap.reportId).Trim()
$preflightReportId = ([string]$preflight.reportId).Trim()
$smokeReportId = ([string]$smoke.reportId).Trim()

$reportIds = @(
    $readinessReportId.ToLowerInvariant(),
    $bootstrapSmokeReportId.ToLowerInvariant(),
    $preflightReportId.ToLowerInvariant(),
    $smokeReportId.ToLowerInvariant()
)
$duplicatedReportId = $reportIds | Group-Object | Where-Object { $_.Count -gt 1 } | Select-Object -First 1
if ($null -ne $duplicatedReportId) {
    throw "Admin VPS bootstrap smoke evidence report ids must be unique."
}

Assert-ReportIdPrefix -Value $readinessReportId -Name "readiness" -Prefix "admin-vps-bootstrap-smoke-readiness-"
Assert-ReportIdPrefix -Value $bootstrapSmokeReportId -Name "bootstrap smoke" -Prefix "admin-vps-bootstrap-smoke-" -ForbiddenPrefix "admin-vps-bootstrap-smoke-readiness-"
Assert-ReportIdPrefix -Value $preflightReportId -Name "preflight" -Prefix "admin-vps-smoke-preflight-"
Assert-ReportIdPrefix -Value $smokeReportId -Name "smoke" -Prefix "admin-vps-smoke-" -ForbiddenPrefix "admin-vps-smoke-preflight-"

Assert-ReportIdTimestampMatches -Value $readinessReportId -Name "readiness" -Prefix "admin-vps-bootstrap-smoke-readiness-" -ExpectedAt $readinessGeneratedAt -TimestampField "generatedAt"
Assert-ReportIdTimestampMatches -Value $bootstrapSmokeReportId -Name "bootstrap smoke" -Prefix "admin-vps-bootstrap-smoke-" -ExpectedAt $bootstrapGeneratedAt -TimestampField "generatedAt"
Assert-ReportIdTimestampMatches -Value $preflightReportId -Name "preflight" -Prefix "admin-vps-smoke-preflight-" -ExpectedAt $preflightGeneratedAt -TimestampField "generatedAt"
Assert-ReportIdTimestampMatches -Value $smokeReportId -Name "smoke" -Prefix "admin-vps-smoke-" -ExpectedAt $smokeStartedAt -TimestampField "startedAt"

$readinessReportSha256 = Get-FileSha256 $readinessFullPath
$bootstrapSmokeReportSha256 = Get-FileSha256 $bootstrapFullPath
$preflightReportSha256 = Get-FileSha256 $preflightFullPath
$smokeReportSha256 = Get-FileSha256 $smokeFullPath

Assert-ExpectedSha256 -Actual $readinessReportSha256 -Expected $ExpectedReadinessReportSha256 -Name "readinessReportSha256"
Assert-ExpectedSha256 -Actual $bootstrapSmokeReportSha256 -Expected $ExpectedBootstrapSmokeReportSha256 -Name "bootstrapSmokeReportSha256"
Assert-ExpectedSha256 -Actual $preflightReportSha256 -Expected $ExpectedPreflightReportSha256 -Name "preflightReportSha256"
Assert-ExpectedSha256 -Actual $smokeReportSha256 -Expected $ExpectedSmokeReportSha256 -Name "smokeReportSha256"

if (($bootstrapCompletedAt - $readinessGeneratedAt) -gt [TimeSpan]::FromMinutes($MaxEvidenceChainMinutes)) {
    throw "Admin VPS bootstrap smoke evidence chain duration exceeds MaxEvidenceChainMinutes ($MaxEvidenceChainMinutes)."
}

$summary = [ordered]@{
    readinessReportId = $readinessReportId
    bootstrapSmokeReportId = $bootstrapSmokeReportId
    preflightReportId = $preflightReportId
    smokeReportId = $smokeReportId
    readinessReportSha256 = $readinessReportSha256
    bootstrapSmokeReportSha256 = $bootstrapSmokeReportSha256
    preflightReportSha256 = $preflightReportSha256
    smokeReportSha256 = $smokeReportSha256
    environmentName = $bootstrap.environmentName
    releaseId = $bootstrap.releaseId
    apiBaseUrl = $bootstrap.apiBaseUrl
    adminWebUrl = $bootstrap.adminWebUrl
    adminEmail = $bootstrap.adminEmail
    operator = $bootstrap.operator
    provider = $bootstrap.provider
    localSqlite = $bootstrap.localSqlite
    passwordEnvName = $bootstrap.passwordEnvName
    passwordEnvPresent = $bootstrap.passwordEnvPresent
    passwordLengthOk = $readiness.passwordLengthOk
    connectionStringPresent = $readiness.connectionStringPresent
    applyMigrations = $readiness.applyMigrations
    confirmBootstrapReset = $readiness.confirmBootstrapReset
    bootstrapResetConfirmed = $bootstrap.bootstrapResetConfirmed
    readyForBootstrapSmoke = $readiness.readyForBootstrapSmoke
    bootstrapStatus = $bootstrap.status
    readinessGeneratedAt = $readiness.generatedAt
    preflightGeneratedAt = $preflight.generatedAt
    smokeStartedAt = $smoke.startedAt
    smokeCompletedAt = $smoke.completedAt
    bootstrapGeneratedAt = $bootstrap.generatedAt
    bootstrapCompletedAt = $bootstrap.completedAt
    preflightToSmokeSeconds = [int][Math]::Round(($smokeStartedAt - $preflightGeneratedAt).TotalSeconds)
    smokeDurationSeconds = [int][Math]::Round(($smokeCompletedAt - $smokeStartedAt).TotalSeconds)
    bootstrapDurationSeconds = [int][Math]::Round(($bootstrapCompletedAt - $bootstrapGeneratedAt).TotalSeconds)
    readinessToBootstrapSeconds = [int][Math]::Round(($bootstrapCompletedAt - $readinessGeneratedAt).TotalSeconds)
    readinessToPreflightSeconds = [int][Math]::Round(($preflightGeneratedAt - $readinessGeneratedAt).TotalSeconds)
    smokeToBootstrapSeconds = [int][Math]::Round(($bootstrapGeneratedAt - $smokeCompletedAt).TotalSeconds)
    evidenceChainDurationSeconds = [int][Math]::Round(($bootstrapCompletedAt - $readinessGeneratedAt).TotalSeconds)
    evidenceChronology = "readiness|preflight|smoke|bootstrap"
    maxEvidenceChainMinutes = $MaxEvidenceChainMinutes
    sectionsContractPath = $sectionsContractPath
    sections = @($smoke.sections).Count
    passed = @($smoke.sections | Where-Object { $_.status -eq "passed" }).Count
    failed = @($smoke.sections | Where-Object { $_.status -eq "failed" }).Count
    blocked = @($smoke.sections | Where-Object { $_.status -eq "blocked" }).Count
    skipped = @($smoke.sections | Where-Object { $_.status -eq "skipped" }).Count
    readinessReportPath = $ReadinessReportPath
    bootstrapSmokeReportPath = $BootstrapSmokeReportPath
    preflightReportPath = $bootstrap.preflightReportPath
    smokeReportPath = $bootstrap.smokeReportPath
}

Write-Host "admin vps bootstrap smoke evidence valid $($summary | ConvertTo-Json -Compress)"
