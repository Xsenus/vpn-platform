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
$checklistValidatorPath = Resolve-RepoPath "scripts/validate-production-evidence-handoff-checklist.ps1"
$tmpDirectory = Resolve-RepoPath "tmp"
New-Item -ItemType Directory -Force -Path $tmpDirectory | Out-Null

$bundleDirectory = Join-Path $tmpDirectory "production-evidence-handoff-checklist-markdown-gates-guard"
$manifestPath = Join-Path $bundleDirectory "production-evidence-manifest.json"
$archivePath = Join-Path $bundleDirectory "production-evidence.zip"
$receiptPath = Join-Path $bundleDirectory "production-evidence-handoff-receipt.json"
$checklistPath = Join-Path $bundleDirectory "production-evidence-handoff-checklist.json"
$checklistMarkdownPath = Join-Path $bundleDirectory "production-evidence-handoff-checklist.md"

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
        -Operator "production-evidence-handoff-checklist-markdown-gates-guard" | Out-Host

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

    $checklist = Get-Content -LiteralPath $checklistPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $gateMessage = [string]$checklist.gates[0].message
    $markdown = Get-Content -LiteralPath $checklistMarkdownPath -Raw -Encoding UTF8
    $tamperedMarkdown = $markdown.Replace($gateMessage, "Gate message deliberately removed for regression.")
    [System.IO.File]::WriteAllText($checklistMarkdownPath, $tamperedMarkdown, [System.Text.UTF8Encoding]::new($false))

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $checklistValidatorPath `
        -ChecklistPath $checklistPath `
        -ReceiptPath $receiptPath `
        -ArchivePath $archivePath 2>&1
    $validatorExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference

    if ($validatorExitCode -eq 0) {
        throw "Production evidence handoff checklist validator accepted tampered gate markdown."
    }

    $text = [string]::Join("`n", @($output | ForEach-Object { [string]$_ }))
    if ($text.IndexOf("Production evidence handoff checklist markdown is missing gate detail:", [System.StringComparison]::OrdinalIgnoreCase) -lt 0 -or
        $text.IndexOf($gateMessage, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Production evidence handoff checklist validator failed for an unexpected reason: $text"
    }

    Write-Output "production evidence handoff checklist markdown gates guard valid"
}
finally {
    if (Test-Path -LiteralPath $bundleDirectory) {
        Remove-Item -LiteralPath $bundleDirectory -Recurse -Force
    }
}
