param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$validatorPath = Resolve-RepoPath "scripts/validate-production-evidence-handoff-package-archive-flow-result.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$resultPath = Join-Path $tmpDirectory "production-evidence-handoff-package-archive-flow-result-stale-release-guard.json"
$fakeHash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"

try {
    $result = [ordered]@{
        status = "passed"
        regressionStatus = "passed"
        releaseId = "stale-release-id"
        packageStatus = "production-ready-handoff"
        productionReady = $true
        resultMarkdownPath = "tmp/production-evidence-handoff-package-archive-flow-result.md"
        productionEvidenceArchivePath = "tmp/production-evidence.zip"
        handoffPackageArchivePath = "tmp/production-evidence-handoff-package.zip"
        productionEvidenceArchiveSha256 = $fakeHash
        handoffPackageArchiveSha256 = $fakeHash
        testedFailures = @(
            [ordered]@{ name = "wrong-expected-sha256"; status = "passed" },
            [ordered]@{ name = "unexpected-entry"; status = "passed" },
            [ordered]@{ name = "missing-required-entry"; status = "passed" }
        )
    }

    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $resultPath -Encoding UTF8

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $validatorPath -ResultJsonPath $resultPath -RequireProductionReady 2>&1
    $validatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($validatorExitCode -eq 0) {
        throw "Validator accepted stale releaseId in -RequireProductionReady mode."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("must match latest active release", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Validator failed for an unexpected reason: $text"
    }

    Write-Output "production evidence handoff package archive flow result latest release guard valid"
}
finally {
    if (Test-Path -LiteralPath $resultPath) {
        Remove-Item -LiteralPath $resultPath -Force
    }
}
