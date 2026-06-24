param(
    [Parameter(Mandatory = $true)]
    [string]$ReportPath,

    [switch]$RequireReady
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ReportPath -PathType Leaf)) {
    throw "Admin VPS smoke preflight report was not found: $ReportPath"
}

$requiredChecks = @(
    "api-base-url",
    "admin-web-url",
    "admin-email",
    "password-env-present",
    "frontend-directory",
    "package-command",
    "browser-runner",
    "report-validator",
    "preflight-validator",
    "remote-latest-release"
)

$secretMarkers = @(
    "authorization:",
    "bearer ",
    "cookie:",
    "set-cookie:",
    ".env",
    "client_secret",
    "api_key",
    "private header",
    "x-api-key",
    "secretkey",
    "webhook secret",
    "vps_ssh_key",
    "x-telegram-bot-api-secret-token",
    "begin private key",
    "begin rsa private key",
    "begin openssh private key"
)

function Assert-ReportHttpUrl {
    param(
        [AllowEmptyString()][string]$Value,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $parsed = $null
    $isInvalid = [string]::IsNullOrWhiteSpace($Value) `
        -or -not [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$parsed) `
        -or ($parsed.Scheme -ne "http" -and $parsed.Scheme -ne "https")

    if ($isInvalid) {
        throw "Admin VPS smoke preflight report field $Name must be an absolute http or https URL."
    }
}

$raw = Get-Content -LiteralPath $ReportPath -Raw -Encoding UTF8
$lowerRaw = $raw.ToLowerInvariant()
foreach ($marker in $secretMarkers) {
    if ($lowerRaw.Contains($marker)) {
        throw "Admin VPS smoke preflight report contains forbidden secret marker: $marker"
    }
}

try {
    $report = $raw | ConvertFrom-Json
}
catch {
    throw "Admin VPS smoke preflight report is not valid JSON: $($_.Exception.Message)"
}

foreach ($propertyName in @("reportId", "generatedAt", "environmentName", "apiBaseUrl", "adminWebUrl", "adminEmail", "smokeReportPath", "preflightReportPath")) {
    if (-not $report.PSObject.Properties.Name.Contains($propertyName)) {
        throw "Admin VPS smoke preflight report is missing required field: $propertyName"
    }

    if ([string]::IsNullOrWhiteSpace([string]$report.$propertyName)) {
        throw "Admin VPS smoke preflight report field is empty: $propertyName"
    }
}

foreach ($propertyName in @("operator", "passwordEnvPresent", "readyForLiveSmoke", "checks")) {
    if (-not $report.PSObject.Properties.Name.Contains($propertyName)) {
        throw "Admin VPS smoke preflight report is missing required field: $propertyName"
    }
}

foreach ($propertyName in @("releaseId", "remoteReleaseId", "remoteReleaseCheckRequired", "remoteReleaseMatched", "remoteReleaseStatus", "remoteReleaseMessage")) {
    if (-not $report.PSObject.Properties.Name.Contains($propertyName)) {
        throw "Admin VPS smoke preflight report is missing required field: $propertyName"
    }
}

if ([string]::IsNullOrWhiteSpace([string]$report.releaseId)) {
    throw "Admin VPS smoke preflight report field is empty: releaseId"
}

if ([string]::IsNullOrWhiteSpace([string]$report.remoteReleaseStatus)) {
    throw "Admin VPS smoke preflight report field is empty: remoteReleaseStatus"
}

if ([string]::IsNullOrWhiteSpace([string]$report.remoteReleaseMessage)) {
    throw "Admin VPS smoke preflight report field is empty: remoteReleaseMessage"
}

Assert-ReportHttpUrl -Value ([string]$report.apiBaseUrl) -Name "apiBaseUrl"
Assert-ReportHttpUrl -Value ([string]$report.adminWebUrl) -Name "adminWebUrl"

if (-not ([string]$report.adminEmail).Contains("@")) {
    throw "Admin VPS smoke preflight report field adminEmail must contain an email address."
}

$generatedAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$report.generatedAt, [ref]$generatedAt)) {
    throw "Admin VPS smoke preflight report field generatedAt is not a valid DateTimeOffset."
}

foreach ($booleanName in @("passwordEnvPresent", "readyForLiveSmoke")) {
    if ($report.$booleanName -isnot [bool]) {
        throw "Admin VPS smoke preflight report field $booleanName must be boolean."
    }

    if ($RequireReady -and -not $report.$booleanName) {
        throw "Admin VPS smoke preflight report field $booleanName must be true when -RequireReady is used."
    }
}

foreach ($booleanName in @("remoteReleaseCheckRequired", "remoteReleaseMatched")) {
    if ($report.$booleanName -isnot [bool]) {
        throw "Admin VPS smoke preflight report field $booleanName must be boolean."
    }
}

$allowedRemoteReleaseStatuses = @("not-required", "matched", "mismatch", "unavailable")
$remoteReleaseStatus = [string]$report.remoteReleaseStatus
if ($allowedRemoteReleaseStatuses -notcontains $remoteReleaseStatus) {
    throw "Admin VPS smoke preflight report field remoteReleaseStatus is invalid: $remoteReleaseStatus"
}

if (-not $report.remoteReleaseCheckRequired) {
    if ($remoteReleaseStatus -ne "not-required") {
        throw "Admin VPS smoke preflight report field remoteReleaseStatus must be not-required when remote release check is disabled."
    }

    if (-not $report.remoteReleaseMatched) {
        throw "Admin VPS smoke preflight report field remoteReleaseMatched must be true when remote release check is disabled."
    }
}
elseif ($report.remoteReleaseMatched) {
    if ($remoteReleaseStatus -ne "matched") {
        throw "Admin VPS smoke preflight report field remoteReleaseStatus must be matched when remoteReleaseMatched is true."
    }

    if ([string]::IsNullOrWhiteSpace([string]$report.remoteReleaseId) -or [string]$report.remoteReleaseId -ne [string]$report.releaseId) {
        throw "Admin VPS smoke preflight report field remoteReleaseId must equal releaseId when remote release is matched."
    }
}
else {
    if ($RequireReady) {
        throw "Admin VPS smoke preflight report field remoteReleaseMatched must be true when -RequireReady is used."
    }

    if ($remoteReleaseStatus -eq "matched" -or $remoteReleaseStatus -eq "not-required") {
        throw "Admin VPS smoke preflight report field remoteReleaseStatus must explain the failed remote release check."
    }

    if ($remoteReleaseStatus -eq "mismatch" -and ([string]::IsNullOrWhiteSpace([string]$report.remoteReleaseId) -or [string]$report.remoteReleaseId -eq [string]$report.releaseId)) {
        throw "Admin VPS smoke preflight report field remoteReleaseId must contain the mismatched remote release."
    }

    if ($remoteReleaseStatus -eq "unavailable" -and -not [string]::IsNullOrWhiteSpace([string]$report.remoteReleaseId)) {
        throw "Admin VPS smoke preflight report field remoteReleaseId must be empty when remote release is unavailable."
    }
}

if ($null -eq $report.checks -or $report.checks.Count -eq 0) {
    throw "Admin VPS smoke preflight report must contain checks array."
}

$checkNames = @($report.checks | ForEach-Object { [string]$_.name })
foreach ($check in $requiredChecks) {
    if ($checkNames -notcontains $check) {
        throw "Admin VPS smoke preflight report is missing check: $check"
    }
}

$duplicates = $checkNames | Group-Object | Where-Object { $_.Count -gt 1 }
if ($duplicates) {
    throw "Admin VPS smoke preflight report contains duplicated check: $($duplicates[0].Name)"
}

foreach ($entry in $report.checks) {
    $name = [string]$entry.name
    if ($requiredChecks -notcontains $name) {
        throw "Admin VPS smoke preflight report contains unsupported check: $name"
    }

    if ($entry.PSObject.Properties.Name -notcontains "passed" -or $entry.passed -isnot [bool]) {
        throw "Admin VPS smoke preflight report check $name must contain boolean passed."
    }

    if ([string]::IsNullOrWhiteSpace([string]$entry.message)) {
        throw "Admin VPS smoke preflight report check $name must contain message."
    }

    if ($RequireReady -and -not $entry.passed) {
        throw "Admin VPS smoke preflight report check $name must be passed when -RequireReady is used."
    }
}

$summary = [ordered]@{
    reportId = $report.reportId
    environmentName = $report.environmentName
    releaseId = $report.releaseId
    checks = $checkNames.Count
    readyForLiveSmoke = $report.readyForLiveSmoke
    passwordEnvPresent = $report.passwordEnvPresent
    remoteReleaseStatus = $report.remoteReleaseStatus
    preflightReportPath = $report.preflightReportPath
}

Write-Host "admin vps smoke preflight report valid $($summary | ConvertTo-Json -Compress)"
