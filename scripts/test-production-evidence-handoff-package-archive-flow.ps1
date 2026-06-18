param(
    [string]$OutputDirectory = "",
    [string]$ApiBaseUrl = "https://api.example.test",
    [string]$PublicWebUrl = "https://example.test",
    [string]$CabinetWebUrl = "https://example.test/cabinet",
    [string]$AdminWebUrl = "https://example.test/admin",
    [string]$X3uiPanelUrl = "https://x3ui.example.test",
    [string]$EnvironmentName = "staging",
    [string]$Operator = "local-test",
    [switch]$RequireProductionReady,
    [switch]$Force,
    [switch]$WriteJson
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $repoRoot = Split-Path -Parent $PSScriptRoot
    return Join-Path $repoRoot $RelativePath
}

function Get-FileSha256 {
    param([string]$PathValue)

    return (Get-FileHash -LiteralPath $PathValue -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Write-Utf8NoBomFile {
    param(
        [string]$PathValue,
        [string]$Content
    )

    [System.IO.File]::WriteAllText($PathValue, $Content, [System.Text.UTF8Encoding]::new($false))
}

function ConvertTo-FlowMarkdown {
    param([object]$Result)

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("# Production evidence handoff package archive flow")
    $lines.Add("")
    $lines.Add("- Status: ``$($Result.status)``")
    $lines.Add("- Release: ``$($Result.releaseId)``")
    $lines.Add("- Package status: ``$($Result.packageStatus)``")
    $lines.Add("- Production ready: ``$($Result.productionReady)``")
    $lines.Add("- Output directory: ``$($Result.outputDirectory)``")
    $lines.Add("- Production evidence archive SHA256: ``$($Result.productionEvidenceArchiveSha256)``")
    $lines.Add("- Handoff package archive SHA256: ``$($Result.handoffPackageArchiveSha256)``")
    $lines.Add("- Regression status: ``$($Result.regressionStatus)``")
    $lines.Add("")
    $lines.Add("## Tested failures")
    foreach ($failure in @($Result.testedFailures)) {
        $lines.Add("- ``$($failure.name)``: $($failure.message)")
    }

    $lines.Add("")
    $lines.Add("## Artifacts")
    $lines.Add("- Production evidence archive: ``$($Result.productionEvidenceArchivePath)``")
    $lines.Add("- Handoff package directory: ``$($Result.handoffPackageDirectory)``")
    $lines.Add("- Handoff package archive: ``$($Result.handoffPackageArchivePath)``")

    return ($lines -join [Environment]::NewLine) + [Environment]::NewLine
}

function Assert-SafeOutputDirectory {
    param(
        [string]$PathValue,
        [string]$RepositoryRoot
    )

    $fullPath = [System.IO.Path]::GetFullPath($PathValue).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    $repoPath = [System.IO.Path]::GetFullPath($RepositoryRoot).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    $volumeRoot = [System.IO.Path]::GetPathRoot($fullPath).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    $leafName = Split-Path -Leaf $fullPath

    if ([string]::Equals($fullPath, $volumeRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Production evidence handoff package archive flow output directory must not be a filesystem root: $fullPath"
    }

    if ([string]::Equals($fullPath, $repoPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Production evidence handoff package archive flow output directory must not be the repository root: $fullPath"
    }

    if ($leafName -notlike "*production-evidence*") {
        throw "Production evidence handoff package archive flow output directory must be clearly named for production-evidence artifacts: $fullPath"
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$bundleDirectory = if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    Join-Path $repoRoot "tmp/production-evidence-handoff-package-archive-flow-test"
}
else {
    [System.IO.Path]::GetFullPath($OutputDirectory)
}

Assert-SafeOutputDirectory -PathValue $bundleDirectory -RepositoryRoot $repoRoot

if ((Test-Path -LiteralPath $bundleDirectory) -and -not $Force) {
    throw "Production evidence handoff package archive flow output directory already exists. Pass -Force to overwrite: $bundleDirectory"
}

if (Test-Path -LiteralPath $bundleDirectory) {
    Remove-Item -LiteralPath $bundleDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $bundleDirectory -Force | Out-Null

$bundleArgs = @{
    OutputDirectory = $bundleDirectory
    ApiBaseUrl = $ApiBaseUrl
    PublicWebUrl = $PublicWebUrl
    CabinetWebUrl = $CabinetWebUrl
    AdminWebUrl = $AdminWebUrl
    X3uiPanelUrl = $X3uiPanelUrl
    EnvironmentName = $EnvironmentName
    Operator = $Operator
    RunProductionGate = $true
    Force = $true
}
& (Resolve-RepoPath "scripts/new-production-evidence-bundle.ps1") @bundleArgs | Out-Host

& (Resolve-RepoPath "scripts/new-production-readiness-summary.ps1") `
    -OutputPath (Join-Path $bundleDirectory "production-readiness-summary.md") `
    -ReportPath (Join-Path $bundleDirectory "staging-smoke-report.json") `
    -PaymentProviderReportPath (Join-Path $bundleDirectory "payment-provider-smoke-report.json") `
    -AdminVpsReportPath (Join-Path $bundleDirectory "admin-vps-smoke-report.json") `
    -VpnLiveReportPath (Join-Path $bundleDirectory "vpn-live-smoke-report.json") `
    -Force | Out-Host

& (Resolve-RepoPath "scripts/new-production-evidence-manifest.ps1") `
    -BundleDirectory $bundleDirectory `
    -RequireSummary `
    -Force | Out-Host

& (Resolve-RepoPath "scripts/new-production-evidence-archive.ps1") `
    -ManifestPath (Join-Path $bundleDirectory "production-evidence-manifest.json") `
    -OutputPath (Join-Path $bundleDirectory "production-evidence.zip") `
    -RequireAllFiles `
    -Force | Out-Host

$archivePath = Join-Path $bundleDirectory "production-evidence.zip"
$archiveSha256 = Get-FileSha256 -PathValue $archivePath

& (Resolve-RepoPath "scripts/new-production-evidence-handoff-receipt.ps1") `
    -ArchivePath $archivePath `
    -ExpectedArchiveSha256 $archiveSha256 `
    -RequireAllFiles `
    -Force | Out-Host

& (Resolve-RepoPath "scripts/new-production-evidence-handoff-checklist.ps1") `
    -ReceiptPath (Join-Path $bundleDirectory "production-evidence-handoff-receipt.json") `
    -ExpectedArchiveSha256 $archiveSha256 `
    -RequireAllFiles `
    -Force | Out-Host

$packageArgs = @{
    ChecklistPath = Join-Path $bundleDirectory "production-evidence-handoff-checklist.json"
    ExpectedArchiveSha256 = $archiveSha256
    Force = $true
}
if ($RequireProductionReady) {
    $packageArgs.RequireProductionReady = $true
}

& (Resolve-RepoPath "scripts/new-production-evidence-handoff-package.ps1") @packageArgs | Out-Host

$packageDirectory = Join-Path $bundleDirectory "production-evidence-handoff-package"
$archiveArgs = @{
    PackageDirectory = $packageDirectory
    ExpectedArchiveSha256 = $archiveSha256
    Force = $true
    WriteJson = $true
}
if ($RequireProductionReady) {
    $archiveArgs.RequireProductionReady = $true
}

$packageArchive = & (Resolve-RepoPath "scripts/new-production-evidence-handoff-package-archive.ps1") @archiveArgs | ConvertFrom-Json

$regressionArgs = @{
    ArchivePath = [string]$packageArchive.archivePath
    ExpectedArchiveSha256 = [string]$packageArchive.archiveSha256
    WriteJson = $true
}
if ($RequireProductionReady) {
    $regressionArgs.RequireProductionReady = $true
}

$regression = & (Resolve-RepoPath "scripts/test-production-evidence-handoff-package-archive-validator.ps1") @regressionArgs | ConvertFrom-Json
$flowResultJsonPath = Join-Path $bundleDirectory "production-evidence-handoff-package-archive-flow-result.json"
$flowResultMarkdownPath = Join-Path $bundleDirectory "production-evidence-handoff-package-archive-flow-result.md"

$result = [ordered]@{
    status = "passed"
    outputDirectory = $bundleDirectory
    releaseId = [string]$packageArchive.releaseId
    packageStatus = [string]$packageArchive.packageStatus
    productionReady = [bool]$packageArchive.productionReady
    productionEvidenceArchivePath = $archivePath
    productionEvidenceArchiveSha256 = $archiveSha256
    handoffPackageDirectory = $packageDirectory
    handoffPackageArchivePath = [string]$packageArchive.archivePath
    handoffPackageArchiveSha256 = [string]$packageArchive.archiveSha256
    regressionStatus = [string]$regression.status
    testedFailures = @($regression.testedFailures)
    resultJsonPath = $flowResultJsonPath
    resultMarkdownPath = $flowResultMarkdownPath
}

$resultJson = $result | ConvertTo-Json -Depth 10
Write-Utf8NoBomFile -PathValue $flowResultJsonPath -Content $resultJson
Write-Utf8NoBomFile -PathValue $flowResultMarkdownPath -Content (ConvertTo-FlowMarkdown -Result ([pscustomobject]$result))

$flowResultValidatorArgs = @{
    ResultJsonPath = $flowResultJsonPath
    ResultMarkdownPath = $flowResultMarkdownPath
}

if ($RequireProductionReady) {
    $flowResultValidatorArgs.RequireProductionReady = $true
}

& (Resolve-RepoPath "scripts/validate-production-evidence-handoff-package-archive-flow-result.ps1") @flowResultValidatorArgs | Out-Host

if ($WriteJson) {
    Write-Output $resultJson
}
else {
    Write-Host "production evidence handoff package archive flow passed $($result | ConvertTo-Json -Depth 10 -Compress)"
}
