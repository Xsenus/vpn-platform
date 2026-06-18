param(
    [Parameter(Mandatory = $true)]
    [string]$BundleDirectory,

    [switch]$RequireSummary,
    [switch]$RequireProductionReady,
    [switch]$WriteJson
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $repoRoot = Split-Path -Parent $PSScriptRoot
    return Join-Path $repoRoot $RelativePath
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

$bundleFullPath = Resolve-BundlePath -DirectoryPath $BundleDirectory
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
