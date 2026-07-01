param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][object]$Value
    )

    [System.IO.File]::WriteAllText(
        $Path,
        ($Value | ConvertTo-Json -Depth 10),
        [System.Text.UTF8Encoding]::new($false))
}

$bundleGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-bundle.ps1"
$manifestGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-manifest.ps1"
$archiveGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-archive.ps1"
$receiptGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-handoff-receipt.ps1"
$receiptValidatorPath = Resolve-RepoPath "scripts/validate-production-evidence-handoff-receipt.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$bundleDirectory = Join-Path $tmpDirectory "production-evidence-handoff-receipt-verified-files-guard"
$manifestPath = Join-Path $bundleDirectory "production-evidence-manifest.json"
$archivePath = Join-Path $bundleDirectory "production-evidence.zip"
$receiptPath = Join-Path $bundleDirectory "production-evidence-handoff-receipt.json"

try {
    if (Test-Path -LiteralPath $bundleDirectory) {
        Remove-Item -LiteralPath $bundleDirectory -Recurse -Force
    }

    & powershell -NoProfile -ExecutionPolicy Bypass -File $bundleGeneratorPath `
        -OutputDirectory $bundleDirectory `
        -ApiBaseUrl "https://api.example.test" `
        -AdminWebUrl "https://admin.example.test" `
        -X3uiPanelUrl "https://x3ui.example.test" `
        -PublicWebUrl "https://public.example.test" `
        -CabinetWebUrl "https://cabinet.example.test" `
        -EnvironmentName "staging" `
        -Operator "production-evidence-handoff-receipt-verified-files-guard" | Out-Host

    & powershell -NoProfile -ExecutionPolicy Bypass -File $manifestGeneratorPath `
        -BundleDirectory $bundleDirectory `
        -OutputPath $manifestPath | Out-Host

    & powershell -NoProfile -ExecutionPolicy Bypass -File $archiveGeneratorPath `
        -ManifestPath $manifestPath `
        -OutputPath $archivePath | Out-Host

    & powershell -NoProfile -ExecutionPolicy Bypass -File $receiptGeneratorPath `
        -ArchivePath $archivePath `
        -OutputPath $receiptPath | Out-Host

    $receipt = Get-Content -LiteralPath $receiptPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $receipt.verifiedFiles[0].sha256 = "0000000000000000000000000000000000000000000000000000000000000000"
    Write-JsonFile -Path $receiptPath -Value $receipt

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $receiptValidatorPath `
        -ReceiptPath $receiptPath `
        -ArchivePath $archivePath 2>&1
    $validatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($validatorExitCode -eq 0) {
        throw "Production evidence handoff receipt validator accepted tampered verifiedFiles sha256."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("Production evidence handoff receipt verified file staging-smoke-report.json sha256 does not match archive.", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Production evidence handoff receipt validator failed for an unexpected reason: $text"
    }

    Write-Output "production evidence handoff receipt verified files guard valid"
}
finally {
    if (Test-Path -LiteralPath $bundleDirectory) {
        Remove-Item -LiteralPath $bundleDirectory -Recurse -Force
    }
}
