param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$validatorPath = Resolve-RepoPath "scripts/validate-production-evidence-handoff-checklist.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$checklistPath = Join-Path $tmpDirectory "production-evidence-handoff-checklist-stale-release-guard.json"
$checklistMarkdownPath = [System.IO.Path]::ChangeExtension($checklistPath, ".md")
$fakeHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"

try {
    $checklist = [ordered]@{
        schemaVersion = 1
        checklistId = "production-evidence-handoff-checklist-stale-release-guard"
        generatedAt = "2026-07-01T16:00:00+07:00"
        status = "production-ready-handoff"
        releaseId = "stale-release-id"
        archivePath = "tmp/production-evidence.zip"
        receiptPath = "tmp/production-evidence-handoff-receipt.json"
        summaryJsonPath = "tmp/production-readiness-summary.json"
        archiveSha256 = $fakeHash
        manifestSha256 = $fakeHash
        productionReady = $true
        gates = @(
            [ordered]@{ name = "receipt-validation"; status = "passed"; message = "sanitized passed gate" },
            [ordered]@{ name = "archive-hash"; status = "passed"; message = "sanitized passed gate" },
            [ordered]@{ name = "summary-present"; status = "passed"; message = "sanitized passed gate" },
            [ordered]@{ name = "production-ready"; status = "passed"; message = "sanitized passed gate" }
        )
        operatorActions = @(
            "Attach production-evidence.zip",
            "Attach production-evidence-handoff-receipt.json",
            "Do not attach .env files"
        )
    }

    $checklist | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $checklistPath -Encoding UTF8
    @"
# Production evidence handoff checklist

- Release: stale-release-id
- Archive SHA256: $fakeHash
- Manifest SHA256: $fakeHash
- Operator actions
"@ | Set-Content -LiteralPath $checklistMarkdownPath -Encoding UTF8

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $validatorPath -ChecklistPath $checklistPath -RequireProductionReady 2>&1
    $validatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($validatorExitCode -eq 0) {
        throw "Validator accepted stale releaseId in -RequireProductionReady mode."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("must match latest active release", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Validator failed for an unexpected reason: $text"
    }

    Write-Output "production evidence handoff checklist latest release guard valid"
}
finally {
    foreach ($path in @($checklistPath, $checklistMarkdownPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }
    }
}
