param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$validatorPath = Resolve-RepoPath "scripts/validate-admin-vps-bootstrap-smoke-evidence.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$readinessReportPath = Join-Path $tmpDirectory "admin-vps-bootstrap-smoke-evidence-stale-release-readiness.json"
$bootstrapReportPath = Join-Path $tmpDirectory "admin-vps-bootstrap-smoke-evidence-stale-release-bootstrap.json"

try {
    $readinessReport = [ordered]@{
        releaseId = "stale-release-id"
    }

    $bootstrapReport = [ordered]@{
        releaseId = "stale-release-id"
    }

    $readinessReport | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $readinessReportPath -Encoding UTF8
    $bootstrapReport | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $bootstrapReportPath -Encoding UTF8

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $validatorPath -ReadinessReportPath $readinessReportPath -BootstrapSmokeReportPath $bootstrapReportPath 2>&1
    $validatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($validatorExitCode -eq 0) {
        throw "Validator accepted stale releaseId in admin VPS bootstrap smoke evidence chain."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("must match latest active release", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Validator failed for an unexpected reason: $text"
    }

    Write-Output "admin vps bootstrap smoke evidence latest release guard valid"
}
finally {
    foreach ($path in @($readinessReportPath, $bootstrapReportPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }
    }
}
