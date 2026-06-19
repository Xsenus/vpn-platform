param(
    [string]$OutputDirectory = "tmp/admin-vps-smoke-sections-contract-regression-test"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$outputFullPath = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) {
    [System.IO.Path]::GetFullPath($OutputDirectory)
} else {
    [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
}

function Assert-InWorkspace {
    param([Parameter(Mandatory = $true)][string]$PathValue)

    $workspace = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $target = [System.IO.Path]::GetFullPath($PathValue).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    if (-not $target.StartsWith($workspace, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove path outside workspace: $target"
    }
}

function Copy-Fixture {
    param(
        [Parameter(Mandatory = $true)][string]$ScenarioPath
    )

    $paths = [ordered]@{
        Contract = Join-Path $ScenarioPath "admin-vps-smoke-sections.json"
        Template = Join-Path $ScenarioPath "admin-vps-smoke-report.template.json"
        ReportValidator = Join-Path $ScenarioPath "validate-admin-vps-smoke-report.ps1"
        BrowserSmokeSpec = Join-Path $ScenarioPath "admin-vps-smoke.spec.ts"
        AllScreensSpec = Join-Path $ScenarioPath "all-screens.spec.ts"
        Guide = Join-Path $ScenarioPath "admin-vps-smoke.md"
    }

    Copy-Item -LiteralPath (Join-Path $repoRoot "docs/admin-vps-smoke-sections.json") -Destination $paths.Contract
    Copy-Item -LiteralPath (Join-Path $repoRoot "docs/admin-vps-smoke-report.template.json") -Destination $paths.Template
    Copy-Item -LiteralPath (Join-Path $repoRoot "scripts/validate-admin-vps-smoke-report.ps1") -Destination $paths.ReportValidator
    Copy-Item -LiteralPath (Join-Path $repoRoot "frontend/e2e/admin-vps-smoke.spec.ts") -Destination $paths.BrowserSmokeSpec
    Copy-Item -LiteralPath (Join-Path $repoRoot "frontend/e2e/all-screens.spec.ts") -Destination $paths.AllScreensSpec
    Copy-Item -LiteralPath (Join-Path $repoRoot "docs/admin-vps-smoke.md") -Destination $paths.Guide

    return $paths
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$PathValue,
        [Parameter(Mandatory = $true)]$Value
    )

    $json = $Value | ConvertTo-Json -Depth 12
    [System.IO.File]::WriteAllText(
        $PathValue,
        $json,
        [System.Text.UTF8Encoding]::new($false))
}

function Write-TextFile {
    param(
        [Parameter(Mandatory = $true)][string]$PathValue,
        [Parameter(Mandatory = $true)][string]$Value
    )

    [System.IO.File]::WriteAllText(
        $PathValue,
        $Value,
        [System.Text.UTF8Encoding]::new($false))
}

function Invoke-Scenario {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][int]$ExpectedExitCode,
        [Parameter(Mandatory = $true)][string]$ExpectedMessage,
        [scriptblock]$Mutate
    )

    $scenarioPath = Join-Path $outputFullPath $Name
    New-Item -ItemType Directory -Path $scenarioPath -Force | Out-Null
    $paths = Copy-Fixture -ScenarioPath $scenarioPath

    if ($null -ne $Mutate) {
        & $Mutate $paths
    }

    $validator = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-sections-contract.ps1"
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & powershell -NoProfile -ExecutionPolicy Bypass -File $validator `
            -ContractPath $paths.Contract `
            -TemplatePath $paths.Template `
            -ReportValidatorPath $paths.ReportValidator `
            -BrowserSmokeSpecPath $paths.BrowserSmokeSpec `
            -AllScreensSpecPath $paths.AllScreensSpec `
            -GuidePath $paths.Guide 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
    $text = ($output | Out-String)

    if ($exitCode -ne $ExpectedExitCode) {
        throw "Scenario '$Name' expected exit code $ExpectedExitCode, got $exitCode. Output: $text"
    }

    if ($text.IndexOf($ExpectedMessage, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Scenario '$Name' expected message '$ExpectedMessage'. Output: $text"
    }

    return [ordered]@{
        name = $Name
        exitCode = $exitCode
    }
}

Assert-InWorkspace -PathValue $outputFullPath
if (Test-Path -LiteralPath $outputFullPath) {
    Remove-Item -LiteralPath $outputFullPath -Recurse -Force
}
New-Item -ItemType Directory -Path $outputFullPath -Force | Out-Null

$results = @()

$results += Invoke-Scenario -Name "valid" -ExpectedExitCode 0 -ExpectedMessage "admin vps smoke sections contract valid" -Mutate $null

$results += Invoke-Scenario -Name "duplicate-section" -ExpectedExitCode 1 -ExpectedMessage "duplicated section" -Mutate {
    param($paths)
    $contract = Get-Content -LiteralPath $paths.Contract -Raw -Encoding UTF8 | ConvertFrom-Json
    $contract.sections += $contract.sections[0]
    Write-JsonFile -PathValue $paths.Contract -Value $contract
}

$results += Invoke-Scenario -Name "bad-route" -ExpectedExitCode 1 -ExpectedMessage "route must be /admin/#dashboard" -Mutate {
    param($paths)
    $contract = Get-Content -LiteralPath $paths.Contract -Raw -Encoding UTF8 | ConvertFrom-Json
    $contract.sections[0].route = "/admin/#wrong"
    Write-JsonFile -PathValue $paths.Contract -Value $contract
}

$results += Invoke-Scenario -Name "template-missing-section" -ExpectedExitCode 1 -ExpectedMessage "mismatch between manifest and report template" -Mutate {
    param($paths)
    $template = Get-Content -LiteralPath $paths.Template -Raw -Encoding UTF8 | ConvertFrom-Json
    $template.sections = @($template.sections | Where-Object { $_.id -ne "provisioning" })
    Write-JsonFile -PathValue $paths.Template -Value $template
}

$results += Invoke-Scenario -Name "browser-spec-no-manifest" -ExpectedExitCode 1 -ExpectedMessage "must read admin-vps-smoke-sections.json" -Mutate {
    param($paths)
    $text = Get-Content -LiteralPath $paths.BrowserSmokeSpec -Raw -Encoding UTF8
    $text = $text.Replace("admin-vps-smoke-sections.json", "admin-vps-smoke-sections.disabled.json")
    Write-TextFile -PathValue $paths.BrowserSmokeSpec -Value $text
}

$results += Invoke-Scenario -Name "all-screens-missing-section" -ExpectedExitCode 1 -ExpectedMessage "all-screens spec is missing section: provisioning" -Mutate {
    param($paths)
    $text = Get-Content -LiteralPath $paths.AllScreensSpec -Raw -Encoding UTF8
    $text = $text.Replace("'provisioning'", "'provisioning-disabled'")
    Write-TextFile -PathValue $paths.AllScreensSpec -Value $text
}

Write-Host "admin vps smoke sections contract regression passed $($results | ConvertTo-Json -Compress)"
