param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$validatorPath = Resolve-RepoPath "scripts/validate-production-evidence-handoff-package-archive-ci-summary.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$resultPath = Join-Path $tmpDirectory "production-evidence-handoff-package-archive-ci-summary-stale-release-guard.json"
$summaryPath = Join-Path $tmpDirectory "production-evidence-handoff-package-archive-ci-summary-stale-release-guard.md"

try {
    $result = [ordered]@{
        status = "passed"
        releaseId = "stale-release-id"
        resultJsonPath = $resultPath
        resultMarkdownPath = $summaryPath
        mainFlow = [ordered]@{
            status = "passed"
            resultJsonPath = "tmp/production-evidence-handoff-package-archive-flow-result.json"
            handoffPackageArchivePath = "tmp/production-evidence-handoff-package.zip"
        }
        resultValidatorRegression = [ordered]@{
            status = "passed"
        }
        longPathRegression = [ordered]@{
            status = "passed"
        }
    }

    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $resultPath -Encoding UTF8
    @"
# Production evidence handoff package archive CI regression

- Status: ``passed``
- Release: ``stale-release-id``
- Main flow status: ``passed``
- Result validator regression: ``passed``
- Long path regression: ``passed``
"@ | Set-Content -LiteralPath $summaryPath -Encoding UTF8

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $validatorPath -ResultJsonPath $resultPath -SummaryPath $summaryPath -RequireProductionReady 2>&1
    $validatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($validatorExitCode -eq 0) {
        throw "Validator accepted stale releaseId in -RequireProductionReady mode."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("must match latest active release", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Validator failed for an unexpected reason: $text"
    }

    Write-Output "production evidence handoff package archive CI summary latest release guard valid"
}
finally {
    foreach ($path in @($resultPath, $summaryPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }
    }
    if ((Test-Path -LiteralPath $tmpDirectory) -and -not (Get-ChildItem -LiteralPath $tmpDirectory -Force)) {
        Remove-Item -LiteralPath $tmpDirectory -Force
    }
}
