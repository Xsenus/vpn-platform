param(
    [Parameter(Mandatory = $true)]
    [string]$ReceiptPath,

    [string]$ArchivePath = "",
    [string]$SummaryJsonPath = "",
    [string]$ExpectedArchiveSha256 = "",
    [string]$OutputPath = "",
    [switch]$RequireAllFiles,
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

function Resolve-RequiredFile {
    param(
        [string]$PathValue,
        [string]$Description
    )

    if ([string]::IsNullOrWhiteSpace($PathValue) -or -not (Test-Path -LiteralPath $PathValue -PathType Leaf)) {
        throw "$Description was not found: $PathValue"
    }

    return (Resolve-Path -LiteralPath $PathValue).Path
}

function Get-OptionalSummary {
    param([string]$PathValue)

    if ([string]::IsNullOrWhiteSpace($PathValue)) {
        return $null
    }

    if (-not (Test-Path -LiteralPath $PathValue -PathType Leaf)) {
        return $null
    }

    $raw = Get-Content -LiteralPath $PathValue -Raw -Encoding UTF8
    if ($raw.Contains([char]0xFFFD)) {
        throw "Production readiness summary JSON contains invalid UTF-8 replacement character."
    }

    try {
        return $raw | ConvertFrom-Json
    }
    catch {
        throw "Production readiness summary JSON is invalid: $($_.Exception.Message)"
    }
}

function New-Gate {
    param(
        [string]$Name,
        [string]$Status,
        [string]$Message
    )

    return [ordered]@{
        name = $Name
        status = $Status
        message = $Message
    }
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

$receiptFullPath = Resolve-RequiredFile -PathValue $ReceiptPath -Description "Production evidence handoff receipt"
$receiptDirectory = Split-Path -Parent $receiptFullPath

$archiveArgs = @{
    ReceiptPath = $receiptFullPath
    RequireAllFiles = $RequireAllFiles
    WriteJson = $true
}

if (-not [string]::IsNullOrWhiteSpace($ArchivePath)) {
    $archiveArgs.ArchivePath = $ArchivePath
}

if (-not [string]::IsNullOrWhiteSpace($ExpectedArchiveSha256)) {
    $archiveArgs.ExpectedArchiveSha256 = $ExpectedArchiveSha256
}

$receiptValidationJson = & (Resolve-RepoPath "scripts/validate-production-evidence-handoff-receipt.ps1") @archiveArgs
$receiptValidation = $receiptValidationJson | ConvertFrom-Json
Assert-KnownReleaseId -Value ([string]$receiptValidation.releaseId)

$summaryPathValue = if ([string]::IsNullOrWhiteSpace($SummaryJsonPath)) {
    Join-Path $receiptDirectory "production-readiness-summary.json"
}
else {
    $SummaryJsonPath
}

$summary = Get-OptionalSummary -PathValue $summaryPathValue
$summaryPresent = $null -ne $summary
$summaryStatus = if ($summaryPresent -and $summary.PSObject.Properties.Name.Contains("status")) {
    [string]$summary.status
}
else {
    ""
}

$nonPassedReports = if ($summaryPresent -and $summary.PSObject.Properties.Name.Contains("nonPassedReports")) {
    [int]$summary.nonPassedReports
}
else {
    -1
}

$roadmapBlockers = if ($summaryPresent -and $summary.PSObject.Properties.Name.Contains("roadmapBlockers")) {
    @($summary.roadmapBlockers)
}
else {
    @()
}

$productionReady = $summaryPresent -and $summaryStatus -eq "production-ready" -and $nonPassedReports -eq 0 -and $roadmapBlockers.Count -eq 0
$handoffStatus = if ($productionReady) { "production-ready-handoff" } else { "blocked" }

$gates = @(
    (New-Gate -Name "receipt-validation" -Status "passed" -Message "Receipt and archive are consistent."),
    (New-Gate -Name "archive-hash" -Status "passed" -Message "Archive SHA256 matches the receipt."),
    (New-Gate -Name "summary-present" -Status ($(if ($summaryPresent) { "passed" } else { "blocked" })) -Message ($(if ($summaryPresent) { "Production readiness summary JSON is present." } else { "production-readiness-summary.json was not found." }))),
    (New-Gate -Name "production-ready" -Status ($(if ($productionReady) { "passed" } else { "blocked" })) -Message ($(if ($productionReady) { "Summary confirms production-ready handoff." } else { "Summary does not confirm production-ready handoff." })))
)

$outputJsonPath = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    Join-Path $receiptDirectory "production-evidence-handoff-checklist.json"
}
else {
    $OutputPath
}

if ((Test-Path -LiteralPath $outputJsonPath) -and -not $Force) {
    throw "Output file already exists: $outputJsonPath. Use -Force to overwrite."
}

$outputDirectory = Split-Path -Parent $outputJsonPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path -LiteralPath $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$checklist = [ordered]@{
    schemaVersion = 1
    checklistId = "production-evidence-handoff-checklist-$([DateTimeOffset]::UtcNow.ToString("yyyyMMddHHmmss"))"
    generatedAt = [DateTimeOffset]::UtcNow.ToString("O")
    status = $handoffStatus
    releaseId = [string]$receiptValidation.releaseId
    archivePath = [string]$receiptValidation.archivePath
    receiptPath = $receiptFullPath
    summaryJsonPath = if ($summaryPresent) { (Resolve-Path -LiteralPath $summaryPathValue).Path } else { $summaryPathValue }
    archiveSha256 = [string]$receiptValidation.archiveSha256
    manifestSha256 = [string]$receiptValidation.manifestSha256
    requireAllFiles = [bool]$RequireAllFiles
    requireProductionReady = [bool]$RequireProductionReady
    productionReady = [bool]$productionReady
    gates = $gates
    operatorActions = @(
        "Attach production-evidence.zip.",
        "Attach production-evidence-handoff-receipt.json and .md.",
        "Attach this checklist JSON and Markdown.",
        "Do not attach .env files, cookies, private headers, provider secrets or API keys."
    )
}

$checklistJson = $checklist | ConvertTo-Json -Depth 8
Set-Content -LiteralPath $outputJsonPath -Value $checklistJson -Encoding UTF8

$outputMarkdownPath = [System.IO.Path]::ChangeExtension((Resolve-Path -LiteralPath $outputJsonPath).Path, ".md")
$markdown = @(
    "# Production evidence handoff checklist",
    "",
    "- Status: $handoffStatus",
    "- Release: $($checklist.releaseId)",
    "- Archive SHA256: $($checklist.archiveSha256)",
    "- Manifest SHA256: $($checklist.manifestSha256)",
    "- Receipt: $receiptFullPath",
    "- Summary JSON: $($checklist.summaryJsonPath)",
    "",
    "## Gates",
    "",
    "| Gate | Status | Message |",
    "| --- | --- | --- |"
)

foreach ($gate in $gates) {
    $markdown += "| $($gate.name) | $($gate.status) | $($gate.message) |"
}

$markdown += @(
    "",
    "## Operator actions",
    "",
    "- Attach production-evidence.zip.",
    "- Attach production-evidence-handoff-receipt.json and .md.",
    "- Attach this checklist JSON and Markdown.",
    "- Do not attach .env files, cookies, private headers, provider secrets or API keys."
)

Set-Content -LiteralPath $outputMarkdownPath -Value ($markdown -join [Environment]::NewLine) -Encoding UTF8

if ($RequireProductionReady -and -not $productionReady) {
    throw "Production evidence handoff checklist is blocked: production-ready summary is required."
}

$result = [ordered]@{
    status = "created"
    checklistStatus = $handoffStatus
    checklistJsonPath = (Resolve-Path -LiteralPath $outputJsonPath).Path
    checklistMarkdownPath = $outputMarkdownPath
    releaseId = [string]$checklist.releaseId
    archiveSha256 = [string]$checklist.archiveSha256
    manifestSha256 = [string]$checklist.manifestSha256
    productionReady = [bool]$productionReady
}

if ($WriteJson) {
    Write-Output ($result | ConvertTo-Json -Depth 8)
}
else {
    Write-Host "production evidence handoff checklist created $($result | ConvertTo-Json -Depth 8 -Compress)"
}
