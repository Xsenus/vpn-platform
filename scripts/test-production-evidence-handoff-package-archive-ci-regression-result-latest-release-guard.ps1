param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$validatorPath = Resolve-RepoPath "scripts/validate-production-evidence-handoff-package-archive-ci-regression-result.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$resultPath = Join-Path $tmpDirectory "production-evidence-handoff-package-archive-ci-regression-result-stale-release-guard.json"

try {
    $result = [ordered]@{
        status = "passed"
        releaseId = "stale-release-id"
        resultMarkdownPath = "tmp/production-evidence-handoff-package-archive-ci-regression-result.md"
        resultJsonPath = "tmp/production-evidence-handoff-package-archive-ci-regression-result.json"
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
        ciSummaryValidatorRegression = [ordered]@{
            status = "passed"
            testedFailures = @(
                [ordered]@{ name = "bad-main-flow-status"; status = "passed" },
                [ordered]@{ name = "bad-release-summary"; status = "passed" },
                [ordered]@{ name = "missing-artifact-path"; status = "passed" },
                [ordered]@{ name = "bad-long-path-status"; status = "passed" }
            )
        }
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

    Write-Output "production evidence handoff package archive CI regression result latest release guard valid"
}
finally {
    if (Test-Path -LiteralPath $resultPath) {
        Remove-Item -LiteralPath $resultPath -Force
    }
}
