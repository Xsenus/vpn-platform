param(
    [Parameter(Mandatory = $true)]
    [string]$BundleDirectory,

    [switch]$RequireSummary,
    [switch]$RequireProductionReady,
    [switch]$WriteJson
)

$ErrorActionPreference = "Stop"

if ((Get-Command ConvertFrom-Json).Parameters.ContainsKey("DateKind")) {
    $PSDefaultParameterValues["ConvertFrom-Json:DateKind"] = "String"
}

function Resolve-RepoPath {
    param([string]$RelativePath)

    $repoRoot = Split-Path -Parent $PSScriptRoot
    return Join-Path $repoRoot $RelativePath
}

function Get-LatestActiveReleaseId {
    $releasesPath = Resolve-RepoPath "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $latest = @($releases | Where-Object { $_.isActive } | Sort-Object -Property { [System.DateTimeOffset]::Parse([string]$_.releasedAt, [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::RoundtripKind) } -Descending | Select-Object -First 1)

    if ($latest.Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$latest[0].releaseId)) {
        throw "Latest active release was not found in AppReleases seed."
    }

    return [string]$latest[0].releaseId
}

function Resolve-BundlePath {
    param([string]$DirectoryPath)

    if ([string]::IsNullOrWhiteSpace($DirectoryPath) -or -not (Test-Path -LiteralPath $DirectoryPath -PathType Container)) {
        throw "Production evidence bundle directory was not found: $DirectoryPath"
    }

    return (Resolve-Path -LiteralPath $DirectoryPath).Path
}

function Invoke-BundleValidator {
    param(
        [string]$Name,
        [string]$ValidatorPath,
        [hashtable]$Parameters
    )

    try {
        $output = & $ValidatorPath @Parameters 2>&1
        return [ordered]@{
            name = $Name
            status = "valid"
            validatorPath = $ValidatorPath
            message = ($output | Out-String).Trim()
        }
    }
    catch {
        return [ordered]@{
            name = $Name
            status = "invalid"
            validatorPath = $ValidatorPath
            message = $_.Exception.Message
        }
    }
}

function Test-LatestReleaseId {
    param(
        [string]$Name,
        [string]$Path,
        [string]$LatestReleaseId
    )

    try {
        $report = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        return [ordered]@{
            name = "$Name-release-id"
            status = "invalid"
            validatorPath = $Path
            message = "Production evidence bundle report $Name JSON is invalid: $($_.Exception.Message)"
        }
    }

    $releaseId = [string]$report.releaseId
    if ([string]::IsNullOrWhiteSpace($releaseId)) {
        return [ordered]@{
            name = "$Name-release-id"
            status = "invalid"
            validatorPath = $Path
            message = "Production evidence bundle report $Name releaseId is required."
        }
    }

    if (-not [string]::Equals($releaseId, $LatestReleaseId, [System.StringComparison]::Ordinal)) {
        return [ordered]@{
            name = "$Name-release-id"
            status = "invalid"
            validatorPath = $Path
            message = "Production evidence bundle report $Name releaseId '$releaseId' must match latest active release '$LatestReleaseId' when -RequireProductionReady is used."
        }
    }

    return $null
}

$bundleFullPath = Resolve-BundlePath -DirectoryPath $BundleDirectory
$latestReleaseId = if ($RequireProductionReady) { Get-LatestActiveReleaseId } else { "" }
$requiredReports = [ordered]@{
    "staging-vps" = [ordered]@{
        fileName = "staging-smoke-report.json"
        validator = Resolve-RepoPath "scripts/validate-staging-smoke-report.ps1"
    }
    "payment-providers" = [ordered]@{
        fileName = "payment-provider-smoke-report.json"
        validator = Resolve-RepoPath "scripts/validate-payment-provider-smoke-report.ps1"
    }
    "admin-vps" = [ordered]@{
        fileName = "admin-vps-smoke-report.json"
        validator = Resolve-RepoPath "scripts/validate-admin-vps-smoke-report.ps1"
    }
    "vpn-live" = [ordered]@{
        fileName = "vpn-live-smoke-report.json"
        validator = Resolve-RepoPath "scripts/validate-vpn-live-smoke-report.ps1"
    }
}

$results = @()
$reportPaths = [ordered]@{}
foreach ($entry in $requiredReports.GetEnumerator()) {
    $name = [string]$entry.Key
    $path = Join-Path $bundleFullPath ([string]$entry.Value.fileName)
    $reportPaths[$name] = $path

    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $results += [ordered]@{
            name = $name
            status = "invalid"
            validatorPath = [string]$entry.Value.validator
            message = "Required report file was not found: $path"
        }
        continue
    }

    if ($RequireProductionReady) {
        $releaseIdValidation = Test-LatestReleaseId -Name $name -Path $path -LatestReleaseId $latestReleaseId
        if ($null -ne $releaseIdValidation) {
            $results += $releaseIdValidation
            continue
        }
    }

    $parameters = @{
        ReportPath = $path
    }
    if ($RequireProductionReady) {
        $parameters.RequireAllPassed = $true
    }

    $results += Invoke-BundleValidator -Name $name -ValidatorPath ([string]$entry.Value.validator) -Parameters $parameters
}

$summaryPath = Join-Path $bundleFullPath "production-readiness-summary.md"
$summaryJsonPath = Join-Path $bundleFullPath "production-readiness-summary.json"
$summaryExists = (Test-Path -LiteralPath $summaryPath -PathType Leaf)
$summaryJsonExists = (Test-Path -LiteralPath $summaryJsonPath -PathType Leaf)

if ($RequireSummary -or $RequireProductionReady -or $summaryExists -or $summaryJsonExists) {
    if (-not $summaryExists) {
        $results += [ordered]@{
            name = "production-readiness-summary"
            status = "invalid"
            validatorPath = Resolve-RepoPath "scripts/validate-production-readiness-summary.ps1"
            message = "Production readiness summary markdown was not found: $summaryPath"
        }
    }
    elseif (-not $summaryJsonExists) {
        $results += [ordered]@{
            name = "production-readiness-summary"
            status = "invalid"
            validatorPath = Resolve-RepoPath "scripts/validate-production-readiness-summary.ps1"
            message = "Production readiness summary JSON was not found: $summaryJsonPath"
        }
    }
    else {
        $summaryParameters = @{
            SummaryPath = $summaryPath
            JsonSummaryPath = $summaryJsonPath
            RequireReportFiles = $true
        }
        if ($RequireProductionReady) {
            $summaryParameters.RequireProductionReady = $true
        }

        $results += Invoke-BundleValidator -Name "production-readiness-summary" -ValidatorPath (Resolve-RepoPath "scripts/validate-production-readiness-summary.ps1") -Parameters $summaryParameters
    }
}

$invalidResults = @($results | Where-Object { $_.status -ne "valid" })
$status = if ($invalidResults.Count -eq 0) { "valid" } else { "invalid" }
$summary = [ordered]@{
    status = $status
    bundleDirectory = $bundleFullPath
    requireSummary = [bool]$RequireSummary
    requireProductionReady = [bool]$RequireProductionReady
    reportPaths = $reportPaths
    summaryPath = $summaryPath
    summaryJsonPath = $summaryJsonPath
    results = $results
}

if ($WriteJson) {
    Write-Output ($summary | ConvertTo-Json -Depth 8)
}
else {
    Write-Output ("production evidence bundle valid " + ($summary | ConvertTo-Json -Depth 8 -Compress))
}

if ($invalidResults.Count -gt 0) {
    throw "Production evidence bundle validation failed: $($invalidResults[0].message)"
}
