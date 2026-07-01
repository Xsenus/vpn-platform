param(
    [Parameter(Mandatory = $true)]
    [string]$ReceiptPath,

    [string]$ArchivePath = "",
    [string]$ExpectedArchiveSha256 = "",
    [switch]$RequireAllFiles,
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

function Assert-StringField {
    param(
        [object]$Object,
        [string]$PropertyName,
        [string]$Context
    )

    if (-not $Object.PSObject.Properties.Name.Contains($PropertyName)) {
        throw "$Context is missing required field: $PropertyName"
    }

    if ([string]::IsNullOrWhiteSpace([string]$Object.$PropertyName)) {
        throw "$Context field is empty: $PropertyName"
    }
}

$receiptFullPath = Resolve-RequiredFile -PathValue $ReceiptPath -Description "Production evidence handoff receipt"
if ([System.IO.Path]::GetExtension($receiptFullPath) -ne ".json") {
    throw "Production evidence handoff receipt must be a .json file."
}

$receiptRaw = Get-Content -LiteralPath $receiptFullPath -Raw -Encoding UTF8
if ($receiptRaw.Contains([char]0xFFFD)) {
    throw "Production evidence handoff receipt contains invalid UTF-8 replacement character."
}

try {
    $receipt = $receiptRaw | ConvertFrom-Json
}
catch {
    throw "Production evidence handoff receipt is invalid JSON: $($_.Exception.Message)"
}

foreach ($fieldName in @("schemaVersion", "receiptId", "generatedAt", "status", "releaseId", "archiveName", "archiveSha256", "archiveBytes", "manifestSha256", "requireAllFiles", "entries", "verifiedFiles")) {
    if (-not $receipt.PSObject.Properties.Name.Contains($fieldName)) {
        throw "Production evidence handoff receipt is missing required field: $fieldName"
    }
}

if ([int]$receipt.schemaVersion -ne 1) {
    throw "Production evidence handoff receipt schemaVersion is unsupported: $($receipt.schemaVersion)"
}

foreach ($fieldName in @("receiptId", "status", "releaseId", "archiveName", "archiveSha256", "manifestSha256")) {
    Assert-StringField -Object $receipt -PropertyName $fieldName -Context "Production evidence handoff receipt"
}

if ([string]$receipt.status -ne "ready-for-handoff") {
    throw "Production evidence handoff receipt status must be ready-for-handoff."
}

if (-not ([string]$receipt.archiveSha256 -match "^[0-9a-f]{64}$")) {
    throw "Production evidence handoff receipt archiveSha256 is invalid."
}

if (-not ([string]$receipt.manifestSha256 -match "^[0-9a-f]{64}$")) {
    throw "Production evidence handoff receipt manifestSha256 is invalid."
}

$generatedAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$receipt.generatedAt, [ref]$generatedAt)) {
    throw "Production evidence handoff receipt generatedAt is not a valid DateTimeOffset."
}

$entries = @($receipt.entries | ForEach-Object { [string]$_ })
if ($entries.Count -eq 0) {
    throw "Production evidence handoff receipt entries must not be empty."
}

$verifiedFiles = @($receipt.verifiedFiles)
if ($verifiedFiles.Count -eq 0) {
    throw "Production evidence handoff receipt verifiedFiles must not be empty."
}

foreach ($requiredEntry in @("production-evidence-manifest.json", "staging-smoke-report.json", "payment-provider-smoke-report.json", "admin-vps-smoke-report.json", "vpn-live-smoke-report.json")) {
    if ($entries -notcontains $requiredEntry) {
        throw "Production evidence handoff receipt is missing entry: $requiredEntry"
    }
}

if ($RequireAllFiles) {
    foreach ($requiredEntry in @("production-readiness-summary.md", "production-readiness-summary.json")) {
        if ($entries -notcontains $requiredEntry) {
            throw "Production evidence handoff receipt is missing entry: $requiredEntry"
        }
    }
}

foreach ($file in $verifiedFiles) {
    foreach ($fieldName in @("name", "entryName", "lengthBytes", "sha256")) {
        Assert-StringField -Object $file -PropertyName $fieldName -Context "Production evidence handoff receipt verified file"
    }

    if (-not ([string]$file.sha256 -match "^[0-9a-f]{64}$")) {
        throw "Production evidence handoff receipt verified file $($file.name) sha256 is invalid."
    }
}

$receiptDirectory = Split-Path -Parent $receiptFullPath
$archiveFullPath = if ([string]::IsNullOrWhiteSpace($ArchivePath)) {
    Resolve-RequiredFile -PathValue (Join-Path $receiptDirectory ([string]$receipt.archiveName)) -Description "Production evidence archive"
}
else {
    Resolve-RequiredFile -PathValue $ArchivePath -Description "Production evidence archive"
}

$expectedHash = if ([string]::IsNullOrWhiteSpace($ExpectedArchiveSha256)) {
    [string]$receipt.archiveSha256
}
else {
    $ExpectedArchiveSha256.ToLowerInvariant()
}

if ($expectedHash -ne [string]$receipt.archiveSha256) {
    throw "Production evidence handoff receipt archiveSha256 does not match expected archive hash."
}

$archiveValidationJson = & (Resolve-RepoPath "scripts/validate-production-evidence-archive.ps1") -ArchivePath $archiveFullPath -RequireAllFiles:$RequireAllFiles -ExpectedArchiveSha256 $expectedHash -WriteJson
$archiveValidation = $archiveValidationJson | ConvertFrom-Json

if ([string]$archiveValidation.releaseId -ne [string]$receipt.releaseId) {
    throw "Production evidence handoff receipt releaseId does not match archive."
}

if ([string]$archiveValidation.archiveSha256 -ne [string]$receipt.archiveSha256) {
    throw "Production evidence handoff receipt archiveSha256 does not match archive."
}

if ([int64]$archiveValidation.archiveBytes -ne [int64]$receipt.archiveBytes) {
    throw "Production evidence handoff receipt archiveBytes does not match archive."
}

if ([string]$archiveValidation.manifestSha256 -ne [string]$receipt.manifestSha256) {
    throw "Production evidence handoff receipt manifestSha256 does not match archive."
}

$archiveVerifiedFiles = @($archiveValidation.verifiedFiles)
if ($verifiedFiles.Count -ne $archiveVerifiedFiles.Count) {
    throw "Production evidence handoff receipt verifiedFiles count does not match archive."
}

foreach ($archiveFile in $archiveVerifiedFiles) {
    $entryName = [string]$archiveFile.entryName
    $receiptMatches = @($verifiedFiles | Where-Object { [string]$_.entryName -eq $entryName })
    if ($receiptMatches.Count -eq 0) {
        throw "Production evidence handoff receipt verifiedFiles is missing archive entry: $entryName"
    }

    if ($receiptMatches.Count -gt 1) {
        throw "Production evidence handoff receipt verifiedFiles contains duplicated entry: $entryName"
    }

    $receiptFile = $receiptMatches[0]
    if ([string]$receiptFile.name -ne [string]$archiveFile.name) {
        throw "Production evidence handoff receipt verified file $entryName name does not match archive."
    }

    if ([int64]$receiptFile.lengthBytes -ne [int64]$archiveFile.lengthBytes) {
        throw "Production evidence handoff receipt verified file $entryName lengthBytes does not match archive."
    }

    if ([string]$receiptFile.sha256 -ne [string]$archiveFile.sha256) {
        throw "Production evidence handoff receipt verified file $entryName sha256 does not match archive."
    }
}

$extraVerifiedFile = @($verifiedFiles | Where-Object {
    $entryName = [string]$_.entryName
    -not (@($archiveVerifiedFiles | Where-Object { [string]$_.entryName -eq $entryName }).Count -gt 0)
})
if ($extraVerifiedFile.Count -gt 0) {
    throw "Production evidence handoff receipt verifiedFiles contains unexpected entry: $($extraVerifiedFile[0].entryName)"
}

$archiveEntries = @($archiveValidation.entries | ForEach-Object { [string]$_ })
$missingFromReceipt = @($archiveEntries | Where-Object { $entries -notcontains $_ })
if ($missingFromReceipt.Count -gt 0) {
    throw "Production evidence handoff receipt is missing archive entry: $($missingFromReceipt[0])"
}

$extraReceiptEntry = @($entries | Where-Object { $archiveEntries -notcontains $_ })
if ($extraReceiptEntry.Count -gt 0) {
    throw "Production evidence handoff receipt contains unexpected entry: $($extraReceiptEntry[0])"
}

$markdownPath = [System.IO.Path]::ChangeExtension($receiptFullPath, ".md")
$markdownFullPath = Resolve-RequiredFile -PathValue $markdownPath -Description "Production evidence handoff receipt markdown"
$markdown = Get-Content -LiteralPath $markdownFullPath -Raw -Encoding UTF8
if ($markdown.Contains([char]0xFFFD)) {
    throw "Production evidence handoff receipt markdown contains invalid UTF-8 replacement character."
}

foreach ($requiredText in @([string]$receipt.releaseId, [string]$receipt.archiveSha256, [string]$receipt.manifestSha256, "Production evidence handoff receipt", "Verified files")) {
    if (-not $markdown.Contains($requiredText)) {
        throw "Production evidence handoff receipt markdown is missing: $requiredText"
    }
}

$result = [ordered]@{
    status = "valid"
    receiptPath = $receiptFullPath
    receiptMarkdownPath = $markdownFullPath
    archivePath = $archiveFullPath
    releaseId = [string]$receipt.releaseId
    archiveSha256 = [string]$receipt.archiveSha256
    manifestSha256 = [string]$receipt.manifestSha256
    entries = $entries
    verifiedFiles = @($verifiedFiles | ForEach-Object {
        [ordered]@{
            name = [string]$_.name
            entryName = [string]$_.entryName
            lengthBytes = [int64]$_.lengthBytes
            sha256 = [string]$_.sha256
        }
    })
}

if ($WriteJson) {
    Write-Output ($result | ConvertTo-Json -Depth 8)
}
else {
    Write-Host "production evidence handoff receipt valid $($result | ConvertTo-Json -Depth 8 -Compress)"
}
