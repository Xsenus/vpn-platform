param()

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $root = Split-Path -Parent $PSScriptRoot
    return Join-Path $root $RelativePath
}

$bundleGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-bundle.ps1"
$manifestGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-manifest.ps1"
$archiveGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-archive.ps1"
$receiptGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-handoff-receipt.ps1"
$checklistGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-handoff-checklist.ps1"
$packageGeneratorPath = Resolve-RepoPath "scripts/new-production-evidence-handoff-package.ps1"
$packageValidatorPath = Resolve-RepoPath "scripts/validate-production-evidence-handoff-package.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$bundleDirectory = Join-Path $tmpDirectory "production-evidence-handoff-package-markdown-files-guard"
$manifestPath = Join-Path $bundleDirectory "production-evidence-manifest.json"
$archivePath = Join-Path $bundleDirectory "production-evidence.zip"
$receiptPath = Join-Path $bundleDirectory "production-evidence-handoff-receipt.json"
$checklistPath = Join-Path $bundleDirectory "production-evidence-handoff-checklist.json"
$packageDirectory = Join-Path $bundleDirectory "production-evidence-handoff-package"
$packageIndexPath = Join-Path $packageDirectory "production-evidence-handoff-package-index.json"
$packageMarkdownPath = Join-Path $packageDirectory "production-evidence-handoff-package-index.md"

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
        -Operator "production-evidence-handoff-package-markdown-files-guard" | Out-Host

    & powershell -NoProfile -ExecutionPolicy Bypass -File $manifestGeneratorPath `
        -BundleDirectory $bundleDirectory `
        -OutputPath $manifestPath | Out-Host

    & powershell -NoProfile -ExecutionPolicy Bypass -File $archiveGeneratorPath `
        -ManifestPath $manifestPath `
        -OutputPath $archivePath | Out-Host

    & powershell -NoProfile -ExecutionPolicy Bypass -File $receiptGeneratorPath `
        -ArchivePath $archivePath `
        -OutputPath $receiptPath | Out-Host

    & powershell -NoProfile -ExecutionPolicy Bypass -File $checklistGeneratorPath `
        -ReceiptPath $receiptPath `
        -ArchivePath $archivePath `
        -OutputPath $checklistPath | Out-Host

    & powershell -NoProfile -ExecutionPolicy Bypass -File $packageGeneratorPath `
        -ChecklistPath $checklistPath `
        -OutputDirectory $packageDirectory | Out-Host

    $packageIndex = Get-Content -LiteralPath $packageIndexPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $firstFileSha256 = [string]$packageIndex.files[1].sha256
    $markdown = Get-Content -LiteralPath $packageMarkdownPath -Raw -Encoding UTF8
    $tamperedMarkdown = $markdown.Replace($firstFileSha256, "0000000000000000000000000000000000000000000000000000000000000000")
    [System.IO.File]::WriteAllText($packageMarkdownPath, $tamperedMarkdown, [System.Text.UTF8Encoding]::new($false))

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $packageValidatorPath `
        -PackageDirectory $packageDirectory 2>&1
    $validatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($validatorExitCode -eq 0) {
        throw "Production evidence handoff package validator accepted tampered package index markdown."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("Production evidence handoff package markdown is missing file detail:", [System.StringComparison]::OrdinalIgnoreCase) -lt 0 -or
        $text.IndexOf($firstFileSha256.Substring(0, 16), [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Production evidence handoff package validator failed for an unexpected reason: $text"
    }

    Write-Output "production evidence handoff package markdown files guard valid"
}
finally {
    if (Test-Path -LiteralPath $bundleDirectory) {
        Remove-Item -LiteralPath $bundleDirectory -Recurse -Force
    }

    if ((Test-Path -LiteralPath $tmpDirectory) -and -not (Get-ChildItem -LiteralPath $tmpDirectory -Force)) {
        Remove-Item -LiteralPath $tmpDirectory -Force
    }
}
