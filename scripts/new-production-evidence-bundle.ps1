param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    [Parameter(Mandatory = $true)]
    [string]$ApiBaseUrl,

    [Parameter(Mandatory = $true)]
    [string]$AdminWebUrl,

    [Parameter(Mandatory = $true)]
    [string]$X3uiPanelUrl,

    [string]$PublicWebUrl = "",
    [string]$CabinetWebUrl = "",
    [string]$EnvironmentName = "staging",
    [string]$Operator = "",
    [string]$ReleaseId = "",
    [ValidateSet("sandbox", "live")]
    [string]$PaymentMode = "sandbox",
    [switch]$Force,
    [switch]$RunProductionGate
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $repoRoot = Split-Path -Parent $PSScriptRoot
    return Join-Path $repoRoot $RelativePath
}

function Invoke-RequiredScript {
    param(
        [string]$ScriptPath,
        [hashtable]$Parameters
    )

    & $ScriptPath @Parameters | Out-Host
}

function Assert-KnownReleaseId {
    param([Parameter(Mandatory = $true)][string]$Value)

    $releasesPath = Resolve-RepoPath "backend/src/VpnPlatform.Api/AppReleases/releases.json"
    if (-not (Test-Path -LiteralPath $releasesPath -PathType Leaf)) {
        throw "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json."
    }

    $releases = Get-Content -LiteralPath $releasesPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $matchedRelease = @($releases | Where-Object { [string]$_.releaseId -eq $Value } | Select-Object -First 1)
    if ($matchedRelease.Count -eq 0) {
        throw "ReleaseId must exist in backend/src/VpnPlatform.Api/AppReleases/releases.json."
    }
}

$fullOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
if ((Test-Path -LiteralPath $fullOutputDirectory) -and -not $Force) {
    $existingFiles = @(Get-ChildItem -LiteralPath $fullOutputDirectory -Filter "*.json" -File -ErrorAction SilentlyContinue)
    if ($existingFiles.Count -gt 0) {
        throw "Output directory already contains JSON reports. Pass -Force to overwrite: $fullOutputDirectory"
    }
}

if (-not [string]::IsNullOrWhiteSpace($ReleaseId)) {
    Assert-KnownReleaseId -Value $ReleaseId.Trim()
}

New-Item -ItemType Directory -Path $fullOutputDirectory -Force | Out-Null

$stagingReportPath = Join-Path $fullOutputDirectory "staging-smoke-report.json"
$paymentProviderReportPath = Join-Path $fullOutputDirectory "payment-provider-smoke-report.json"
$adminVpsReportPath = Join-Path $fullOutputDirectory "admin-vps-smoke-report.json"
$vpnLiveReportPath = Join-Path $fullOutputDirectory "vpn-live-smoke-report.json"

$commonParameters = @{
    EnvironmentName = $EnvironmentName
    Operator = $Operator
    ReleaseId = $ReleaseId
}

if ($Force) {
    $commonParameters.Force = $true
}

$stagingParameters = $commonParameters.Clone()
$stagingParameters.OutputPath = $stagingReportPath
$stagingParameters.ApiBaseUrl = $ApiBaseUrl
$stagingParameters.PublicWebUrl = $PublicWebUrl
$stagingParameters.CabinetWebUrl = $CabinetWebUrl
$stagingParameters.AdminWebUrl = $AdminWebUrl
Invoke-RequiredScript -ScriptPath (Resolve-RepoPath "scripts/new-staging-smoke-report.ps1") -Parameters $stagingParameters

$paymentParameters = $commonParameters.Clone()
$paymentParameters.OutputPath = $paymentProviderReportPath
$paymentParameters.Mode = $PaymentMode
Invoke-RequiredScript -ScriptPath (Resolve-RepoPath "scripts/new-payment-provider-smoke-report.ps1") -Parameters $paymentParameters

$adminVpsParameters = $commonParameters.Clone()
$adminVpsParameters.OutputPath = $adminVpsReportPath
$adminVpsParameters.ApiBaseUrl = $ApiBaseUrl
$adminVpsParameters.AdminWebUrl = $AdminWebUrl
Invoke-RequiredScript -ScriptPath (Resolve-RepoPath "scripts/new-admin-vps-smoke-report.ps1") -Parameters $adminVpsParameters

$vpnLiveParameters = $commonParameters.Clone()
$vpnLiveParameters.OutputPath = $vpnLiveReportPath
$vpnLiveParameters.ApiBaseUrl = $ApiBaseUrl
$vpnLiveParameters.AdminWebUrl = $AdminWebUrl
$vpnLiveParameters.X3uiPanelUrl = $X3uiPanelUrl
Invoke-RequiredScript -ScriptPath (Resolve-RepoPath "scripts/new-vpn-live-smoke-report.ps1") -Parameters $vpnLiveParameters

$gateStatus = "not-run"
$gateMessage = ""
if ($RunProductionGate) {
    $gateScript = Resolve-RepoPath "scripts/assert-production-readiness.ps1"
    try {
        $gateOutput = & $gateScript `
            -ReportPath $stagingReportPath `
            -PaymentProviderReportPath $paymentProviderReportPath `
            -AdminVpsReportPath $adminVpsReportPath `
            -VpnLiveReportPath $vpnLiveReportPath 2>&1

        $gateMessage = ($gateOutput | Out-String).Trim()
        $gateStatus = "passed"
    }
    catch {
        $gateStatus = "blocked"
        $gateMessage = $_.Exception.Message
    }
}

$summary = [ordered]@{
    status = "generated"
    outputDirectory = $fullOutputDirectory
    stagingReportPath = $stagingReportPath
    paymentProviderReportPath = $paymentProviderReportPath
    adminVpsReportPath = $adminVpsReportPath
    vpnLiveReportPath = $vpnLiveReportPath
    productionGateStatus = $gateStatus
    productionGateMessage = $gateMessage
}

Write-Output ("production evidence bundle generated " + ($summary | ConvertTo-Json -Depth 6 -Compress))
