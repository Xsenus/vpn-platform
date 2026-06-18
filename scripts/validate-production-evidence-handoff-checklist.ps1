param(
    [Parameter(Mandatory = $true)]
    [string]$ChecklistPath,

    [string]$ReceiptPath = "",
    [string]$ArchivePath = "",
    [string]$SummaryJsonPath = "",
    [string]$ExpectedArchiveSha256 = "",
    [switch]$RequireProductionReady,
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

function Read-JsonFile {
    param(
        [string]$PathValue,
        [string]$Description
    )

    $fullPath = Resolve-RequiredFile -PathValue $PathValue -Description $Description
    $raw = Get-Content -LiteralPath $fullPath -Raw -Encoding UTF8
    if ($raw.Contains([char]0xFFFD)) {
        throw "$Description contains invalid UTF-8 replacement character."
    }

    try {
        return [ordered]@{
            path = $fullPath
            json = $raw | ConvertFrom-Json
        }
    }
    catch {
        throw "$Description is invalid JSON: $($_.Exception.Message)"
    }
}

$checklistFile = Read-JsonFile -PathValue $ChecklistPath -Description "Production evidence handoff checklist"
$checklistFullPath = [string]$checklistFile.path
$checklist = $checklistFile.json

foreach ($fieldName in @("schemaVersion", "checklistId", "generatedAt", "status", "releaseId", "archivePath", "receiptPath", "summaryJsonPath", "archiveSha256", "manifestSha256", "productionReady", "gates", "operatorActions")) {
    if (-not $checklist.PSObject.Properties.Name.Contains($fieldName)) {
        throw "Production evidence handoff checklist is missing required field: $fieldName"
    }
}

if ([int]$checklist.schemaVersion -ne 1) {
    throw "Production evidence handoff checklist schemaVersion is unsupported: $($checklist.schemaVersion)"
}

foreach ($fieldName in @("checklistId", "status", "releaseId", "archivePath", "receiptPath", "summaryJsonPath", "archiveSha256", "manifestSha256")) {
    Assert-StringField -Object $checklist -PropertyName $fieldName -Context "Production evidence handoff checklist"
}

if (@("blocked", "production-ready-handoff") -notcontains [string]$checklist.status) {
    throw "Production evidence handoff checklist status is invalid: $($checklist.status)"
}

if (-not ([string]$checklist.archiveSha256 -match "^[0-9a-f]{64}$")) {
    throw "Production evidence handoff checklist archiveSha256 is invalid."
}

if (-not ([string]$checklist.manifestSha256 -match "^[0-9a-f]{64}$")) {
    throw "Production evidence handoff checklist manifestSha256 is invalid."
}

$generatedAt = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$checklist.generatedAt, [ref]$generatedAt)) {
    throw "Production evidence handoff checklist generatedAt is not a valid DateTimeOffset."
}

$gates = @($checklist.gates)
if ($gates.Count -eq 0) {
    throw "Production evidence handoff checklist gates must not be empty."
}

foreach ($requiredGate in @("receipt-validation", "archive-hash", "summary-present", "production-ready")) {
    $match = @($gates | Where-Object { [string]$_.name -eq $requiredGate })
    if ($match.Count -ne 1) {
        throw "Production evidence handoff checklist is missing gate: $requiredGate"
    }
}

foreach ($gate in $gates) {
    foreach ($fieldName in @("name", "status", "message")) {
        Assert-StringField -Object $gate -PropertyName $fieldName -Context "Production evidence handoff checklist gate"
    }

    if (@("passed", "blocked") -notcontains [string]$gate.status) {
        throw "Production evidence handoff checklist gate $($gate.name) status is invalid."
    }
}

$operatorActions = @($checklist.operatorActions | ForEach-Object { [string]$_ })
if ($operatorActions.Count -eq 0) {
    throw "Production evidence handoff checklist operatorActions must not be empty."
}

$joinedOperatorActions = $operatorActions -join "`n"
foreach ($requiredText in @("Attach production-evidence.zip", "Attach production-evidence-handoff-receipt.json", "Do not attach .env files")) {
    if (-not $joinedOperatorActions.Contains($requiredText)) {
        throw "Production evidence handoff checklist operatorActions is missing: $requiredText"
    }
}

$forbiddenSecretMarkers = @("BEGIN PRIVATE KEY", "client_secret", "api_key", "Authorization:", "Cookie:", "Set-Cookie:", "password=")
foreach ($marker in $forbiddenSecretMarkers) {
    if ($joinedOperatorActions.Contains($marker)) {
        throw "Production evidence handoff checklist operatorActions contains forbidden secret marker: $marker"
    }
}

$receiptFullPath = if ([string]::IsNullOrWhiteSpace($ReceiptPath)) {
    Resolve-RequiredFile -PathValue ([string]$checklist.receiptPath) -Description "Production evidence handoff receipt"
}
else {
    Resolve-RequiredFile -PathValue $ReceiptPath -Description "Production evidence handoff receipt"
}

$expectedHash = if ([string]::IsNullOrWhiteSpace($ExpectedArchiveSha256)) {
    [string]$checklist.archiveSha256
}
else {
    $ExpectedArchiveSha256.ToLowerInvariant()
}

if ($expectedHash -ne [string]$checklist.archiveSha256) {
    throw "Production evidence handoff checklist archiveSha256 does not match expected archive hash."
}

$receiptValidationArgs = @{
    ReceiptPath = $receiptFullPath
    ExpectedArchiveSha256 = $expectedHash
    WriteJson = $true
}

if (-not [string]::IsNullOrWhiteSpace($ArchivePath)) {
    $receiptValidationArgs.ArchivePath = $ArchivePath
}
elseif (-not [string]::IsNullOrWhiteSpace([string]$checklist.archivePath)) {
    $receiptValidationArgs.ArchivePath = [string]$checklist.archivePath
}

$receiptValidationJson = & (Resolve-RepoPath "scripts/validate-production-evidence-handoff-receipt.ps1") @receiptValidationArgs
$receiptValidation = $receiptValidationJson | ConvertFrom-Json

if ([string]$receiptValidation.releaseId -ne [string]$checklist.releaseId) {
    throw "Production evidence handoff checklist releaseId does not match receipt."
}

if ([string]$receiptValidation.archiveSha256 -ne [string]$checklist.archiveSha256) {
    throw "Production evidence handoff checklist archiveSha256 does not match receipt."
}

if ([string]$receiptValidation.manifestSha256 -ne [string]$checklist.manifestSha256) {
    throw "Production evidence handoff checklist manifestSha256 does not match receipt."
}

$summaryFullPath = if ([string]::IsNullOrWhiteSpace($SummaryJsonPath)) {
    [string]$checklist.summaryJsonPath
}
else {
    $SummaryJsonPath
}

$summary = $null
if (-not [string]::IsNullOrWhiteSpace($summaryFullPath) -and (Test-Path -LiteralPath $summaryFullPath -PathType Leaf)) {
    $summaryFile = Read-JsonFile -PathValue $summaryFullPath -Description "Production readiness summary JSON"
    $summary = $summaryFile.json
}

$productionReady = [bool]$checklist.productionReady
if ($RequireProductionReady) {
    if (-not $productionReady) {
        throw "Production evidence handoff checklist is not production-ready."
    }

    if ([string]$checklist.status -ne "production-ready-handoff") {
        throw "Production evidence handoff checklist status must be production-ready-handoff."
    }

    $blockedGate = @($gates | Where-Object { [string]$_.status -ne "passed" })
    if ($blockedGate.Count -gt 0) {
        throw "Production evidence handoff checklist contains blocked gate: $($blockedGate[0].name)"
    }

    if ($null -eq $summary) {
        throw "Production readiness summary JSON is required for production-ready handoff."
    }

    if ([string]$summary.status -ne "production-ready") {
        throw "Production readiness summary status must be production-ready."
    }

    if ([int]$summary.nonPassedReports -ne 0) {
        throw "Production readiness summary nonPassedReports must be 0."
    }

    if (@($summary.roadmapBlockers).Count -ne 0) {
        throw "Production readiness summary roadmapBlockers must be empty."
    }
}

$markdownFullPath = [System.IO.Path]::ChangeExtension($checklistFullPath, ".md")
$markdownFullPath = Resolve-RequiredFile -PathValue $markdownFullPath -Description "Production evidence handoff checklist markdown"
$markdown = Get-Content -LiteralPath $markdownFullPath -Raw -Encoding UTF8
if ($markdown.Contains([char]0xFFFD)) {
    throw "Production evidence handoff checklist markdown contains invalid UTF-8 replacement character."
}

foreach ($requiredText in @([string]$checklist.releaseId, [string]$checklist.archiveSha256, [string]$checklist.manifestSha256, "Production evidence handoff checklist", "Operator actions")) {
    if (-not $markdown.Contains($requiredText)) {
        throw "Production evidence handoff checklist markdown is missing: $requiredText"
    }
}

$result = [ordered]@{
    status = "valid"
    checklistStatus = [string]$checklist.status
    checklistPath = $checklistFullPath
    checklistMarkdownPath = $markdownFullPath
    releaseId = [string]$checklist.releaseId
    archiveSha256 = [string]$checklist.archiveSha256
    manifestSha256 = [string]$checklist.manifestSha256
    productionReady = $productionReady
    gates = @($gates | ForEach-Object {
        [ordered]@{
            name = [string]$_.name
            status = [string]$_.status
        }
    })
}

if ($WriteJson) {
    Write-Output ($result | ConvertTo-Json -Depth 8)
}
else {
    Write-Host "production evidence handoff checklist valid $($result | ConvertTo-Json -Depth 8 -Compress)"
}
