param(
    [Parameter(Mandatory = $true)]
    [string]$ReportPath,

    [string]$SectionsContractPath = "docs/admin-vps-smoke-sections.json",

    [switch]$RequireAllPassed
)

$ErrorActionPreference = "Stop"

if ((Get-Command ConvertFrom-Json).Parameters.ContainsKey("DateKind")) {
    $PSDefaultParameterValues["ConvertFrom-Json:DateKind"] = "String"
}

if (-not (Test-Path -LiteralPath $ReportPath)) {
    throw "Admin VPS smoke report was not found: $ReportPath"
}

$allowedStatuses = @("passed", "failed", "blocked", "skipped")
$placeholderEvidenceMarkers = @(
    "TODO",
    "Not checked yet",
    "safe screenshot name",
    "browser smoke note"
)
$secretMarkers = @(
    "password=",
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
        [string]$Value,
        [string]$Name
    )

    $parsed = $null
    $isInvalid = [string]::IsNullOrWhiteSpace($Value) -or -not [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$parsed) -or ($parsed.Scheme -ne "http" -and $parsed.Scheme -ne "https")
    if ($isInvalid) {
        throw "Admin VPS smoke report field $Name must be an absolute http or https URL."
    }
}

function Resolve-RepoPath {
    param([Parameter(Mandatory = $true)][string]$PathValue)

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return [System.IO.Path]::GetFullPath($PathValue)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $PathValue))
}

function Get-LatestActiveReleaseId {
    $releasesPath = Join-Path $repoRoot "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $latest = @($releases | Where-Object { $_.isActive } | Sort-Object -Property { [System.DateTimeOffset]::Parse([string]$_.releasedAt, [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::RoundtripKind) } -Descending | Select-Object -First 1)

    if ($latest.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$latest[0].releaseId)) {
        throw "Latest active release was not found in AppReleases seed."
    }

    return [string]$latest[0].releaseId
}

function Get-SectionsContract {
    param([Parameter(Mandatory = $true)][string]$PathValue)

    if (-not (Test-Path -LiteralPath $PathValue -PathType Leaf)) {
        throw "Admin VPS smoke report sections contract was not found: $PathValue"
    }

    try {
        $contract = (Get-Content -LiteralPath $PathValue -Raw -Encoding UTF8) | ConvertFrom-Json
    }
    catch {
        throw "Admin VPS smoke report sections contract is not valid JSON: $($_.Exception.Message)"
    }

    if ([string]$contract.contractId -ne "admin-vps-smoke-sections") {
        throw "Admin VPS smoke report sections contractId must be admin-vps-smoke-sections."
    }

    $sections = @($contract.sections)
    if ($sections.Count -eq 0) {
        throw "Admin VPS smoke report sections contract must contain sections."
    }

    $map = [ordered]@{}
    foreach ($section in $sections) {
        $id = [string]$section.id
        $route = [string]$section.route

        if ([string]::IsNullOrWhiteSpace($id)) {
            throw "Admin VPS smoke report sections contract contains section without id."
        }

        if ($map.Contains($id)) {
            throw "Admin VPS smoke report sections contract contains duplicated section: $id"
        }

        $expectedRoute = "/admin/#$id"
        if ($route -ne $expectedRoute) {
            throw "Admin VPS smoke report sections contract route for $id must be $expectedRoute."
        }

        $map[$id] = $route
    }

    return $map
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$reportFullPath = [System.IO.Path]::GetFullPath($ReportPath)
$sectionsContractFullPath = Resolve-RepoPath $SectionsContractPath
$requiredSectionRoutes = Get-SectionsContract -PathValue $sectionsContractFullPath
$requiredSections = @($requiredSectionRoutes.Keys)

$raw = Get-Content -LiteralPath $ReportPath -Raw -Encoding UTF8
$lowerRaw = $raw.ToLowerInvariant()
foreach ($marker in $secretMarkers) {
    if ($lowerRaw.Contains($marker)) {
        throw "Admin VPS smoke report contains forbidden secret marker: $marker"
    }
}

try {
    $report = $raw | ConvertFrom-Json
}
catch {
    throw "Admin VPS smoke report is not valid JSON: $($_.Exception.Message)"
}

foreach ($propertyName in @("reportId", "environmentName", "apiBaseUrl", "adminWebUrl", "adminEmail", "smokeReportPath", "startedAt", "completedAt", "releaseId", "operator", "notes")) {
    if (-not $report.PSObject.Properties.Name.Contains($propertyName)) {
        throw "Admin VPS smoke report is missing required field: $propertyName"
    }

    if ([string]::IsNullOrWhiteSpace([string]$report.$propertyName)) {
        throw "Admin VPS smoke report field is empty: $propertyName"
    }
}

Assert-ReportHttpUrl -Value ([string]$report.apiBaseUrl) -Name "apiBaseUrl"
Assert-ReportHttpUrl -Value ([string]$report.adminWebUrl) -Name "adminWebUrl"

if (-not ([string]$report.adminEmail).Contains("@")) {
    throw "Admin VPS smoke report field adminEmail must contain an email address."
}

if ($RequireAllPassed) {
    $latestReleaseId = Get-LatestActiveReleaseId
    if (-not [string]::Equals([string]$report.releaseId, $latestReleaseId, [System.StringComparison]::Ordinal)) {
        throw "Admin VPS smoke report releaseId '$($report.releaseId)' must match latest active release '$latestReleaseId' when -RequireAllPassed is used."
    }
}

$smokeReportPath = Resolve-RepoPath ([string]$report.smokeReportPath)
if (-not [string]::Equals($smokeReportPath, $reportFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Admin VPS smoke report mismatch for smokeReportPath. Report='$smokeReportPath', actual='$reportFullPath'."
}

$startedAt = [DateTimeOffset]::MinValue
$completedAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$report.startedAt, [ref]$startedAt)) {
    throw "Admin VPS smoke report field startedAt is not a valid DateTimeOffset."
}

if (-not [DateTimeOffset]::TryParse([string]$report.completedAt, [ref]$completedAt)) {
    throw "Admin VPS smoke report field completedAt is not a valid DateTimeOffset."
}

if ($completedAt -lt $startedAt) {
    throw "Admin VPS smoke report completedAt must be greater than or equal to startedAt."
}

foreach ($booleanName in @("accountBootstrapChecked", "adminLoginPassed", "noJsErrors", "noUnauthorizedAfterLogin")) {
    if (-not $report.PSObject.Properties.Name.Contains($booleanName)) {
        throw "Admin VPS smoke report is missing boolean field: $booleanName"
    }

    if ($report.$booleanName -isnot [bool]) {
        throw "Admin VPS smoke report field $booleanName must be boolean."
    }

    if ($RequireAllPassed -and -not $report.$booleanName) {
        throw "Admin VPS smoke report field $booleanName must be true when -RequireAllPassed is used."
    }
}

if ($null -eq $report.sections -or $report.sections.Count -eq 0) {
    throw "Admin VPS smoke report must contain sections array."
}

$sectionIds = @($report.sections | ForEach-Object { [string]$_.id })
foreach ($section in $requiredSections) {
    if ($sectionIds -notcontains $section) {
        throw "Admin VPS smoke report is missing admin section: $section"
    }
}

$duplicates = $sectionIds | Group-Object | Where-Object { $_.Count -gt 1 }
if ($duplicates) {
    throw "Admin VPS smoke report contains duplicated admin section: $($duplicates[0].Name)"
}

foreach ($entry in $report.sections) {
    $section = [string]$entry.id
    if ($requiredSections -notcontains $section) {
        throw "Admin VPS smoke report contains unsupported admin section: $section"
    }

    $status = [string]$entry.status
    if ($allowedStatuses -notcontains $status) {
        throw "Admin VPS smoke report section $section has unsupported status: $status"
    }

    $route = [string]$entry.route
    if ([string]::IsNullOrWhiteSpace($route)) {
        throw "Admin VPS smoke report section $section must contain route."
    }

    if ($route -ne $requiredSectionRoutes[$section]) {
        throw "Admin VPS smoke report section $section route must match sections contract."
    }

    if ($entry.PSObject.Properties.Name -notcontains "httpStatus" -or ($entry.httpStatus -isnot [int] -and $entry.httpStatus -isnot [long])) {
        throw "Admin VPS smoke report section $section must contain integer httpStatus."
    }

    if ($entry.PSObject.Properties.Name -notcontains "loaded" -or $entry.loaded -isnot [bool]) {
        throw "Admin VPS smoke report section $section must contain boolean loaded."
    }

    if ([string]::IsNullOrWhiteSpace([string]$entry.evidence)) {
        throw "Admin VPS smoke report section $section must contain safe evidence."
    }

    if ($RequireAllPassed -and $status -ne "passed") {
        throw "Admin VPS smoke report section $section must be passed when -RequireAllPassed is used."
    }

    if ($RequireAllPassed -and -not $entry.loaded) {
        throw "Admin VPS smoke report section $section must be loaded when -RequireAllPassed is used."
    }

    if ($RequireAllPassed -and ([int]$entry.httpStatus -lt 200 -or [int]$entry.httpStatus -ge 400)) {
        throw "Admin VPS smoke report section $section must contain successful httpStatus when -RequireAllPassed is used."
    }

    if ($RequireAllPassed) {
        $evidence = [string]$entry.evidence
        foreach ($marker in $placeholderEvidenceMarkers) {
            if ($evidence.IndexOf($marker, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                throw "Admin VPS smoke report section $section must contain real evidence without placeholder markers when -RequireAllPassed is used."
            }
        }
    }
}

$summary = [ordered]@{
    reportId = $report.reportId
    environmentName = $report.environmentName
    releaseId = $report.releaseId
    smokeReportPath = $report.smokeReportPath
    sectionsContractPath = $sectionsContractFullPath
    sections = $sectionIds.Count
    passed = @($report.sections | Where-Object { $_.status -eq "passed" }).Count
    failed = @($report.sections | Where-Object { $_.status -eq "failed" }).Count
    blocked = @($report.sections | Where-Object { $_.status -eq "blocked" }).Count
    skipped = @($report.sections | Where-Object { $_.status -eq "skipped" }).Count
}

Write-Host "admin vps smoke report valid $($summary | ConvertTo-Json -Compress)"
