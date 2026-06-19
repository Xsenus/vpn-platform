param(
    [string]$ContractPath = "docs/admin-vps-smoke-sections.json",
    [string]$TemplatePath = "docs/admin-vps-smoke-report.template.json",
    [string]$ReportValidatorPath = "scripts/validate-admin-vps-smoke-report.ps1",
    [string]$BrowserSmokeSpecPath = "frontend/e2e/admin-vps-smoke.spec.ts",
    [string]$AllScreensSpecPath = "frontend/e2e/all-screens.spec.ts",
    [string]$GuidePath = "docs/admin-vps-smoke.md"
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param(
        [Parameter(Mandatory = $true)][string]$PathValue
    )

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return [System.IO.Path]::GetFullPath($PathValue)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $PathValue))
}

function Read-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$PathValue,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (-not (Test-Path -LiteralPath $PathValue -PathType Leaf)) {
        throw "Admin VPS smoke sections contract $Name was not found: $PathValue"
    }

    try {
        return (Get-Content -LiteralPath $PathValue -Raw -Encoding UTF8) | ConvertFrom-Json
    }
    catch {
        throw "Admin VPS smoke sections contract $Name is not valid JSON: $($_.Exception.Message)"
    }
}

function Read-TextFile {
    param(
        [Parameter(Mandatory = $true)][string]$PathValue,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (-not (Test-Path -LiteralPath $PathValue -PathType Leaf)) {
        throw "Admin VPS smoke sections contract $Name was not found: $PathValue"
    }

    return Get-Content -LiteralPath $PathValue -Raw -Encoding UTF8
}

function Assert-Contains {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string]$Needle,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if ($Text.IndexOf($Needle, [System.StringComparison]::Ordinal) -lt 0) {
        throw $Message
    }
}

function Assert-ContainsAny {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string[]]$Needles,
        [Parameter(Mandatory = $true)][string]$Message
    )

    foreach ($needle in $Needles) {
        if ($Text.IndexOf($needle, [System.StringComparison]::Ordinal) -ge 0) {
            return
        }
    }

    throw $Message
}

function Get-SectionMap {
    param(
        [Parameter(Mandatory = $true)]$Sections,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $items = @($Sections)
    if ($items.Count -eq 0) {
        throw "Admin VPS smoke sections contract $Name must contain sections."
    }

    $map = [ordered]@{}
    foreach ($section in $items) {
        $id = [string]$section.id
        $route = [string]$section.route

        if ([string]::IsNullOrWhiteSpace($id)) {
            throw "Admin VPS smoke sections contract $Name contains section without id."
        }

        if ($id -notmatch '^[a-z][a-z0-9-]*$') {
            throw "Admin VPS smoke sections contract $Name contains invalid section id: $id"
        }

        if ($map.Contains($id)) {
            throw "Admin VPS smoke sections contract $Name contains duplicated section: $id"
        }

        if ([string]::IsNullOrWhiteSpace($route)) {
            throw "Admin VPS smoke sections contract $Name section $id must contain route."
        }

        $expectedRoute = "/admin/#$id"
        if ($route -ne $expectedRoute) {
            throw "Admin VPS smoke sections contract $Name section $id route must be $expectedRoute."
        }

        $map[$id] = $route
    }

    return $map
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$contractFullPath = Resolve-RepoPath $ContractPath
$templateFullPath = Resolve-RepoPath $TemplatePath
$reportValidatorFullPath = Resolve-RepoPath $ReportValidatorPath
$browserSmokeSpecFullPath = Resolve-RepoPath $BrowserSmokeSpecPath
$allScreensSpecFullPath = Resolve-RepoPath $AllScreensSpecPath
$guideFullPath = Resolve-RepoPath $GuidePath

$contract = Read-JsonFile -PathValue $contractFullPath -Name "manifest"
if ([string]$contract.contractId -ne "admin-vps-smoke-sections") {
    throw "Admin VPS smoke sections contract manifest contractId must be admin-vps-smoke-sections."
}

$contractSections = Get-SectionMap -Sections $contract.sections -Name "manifest"

$template = Read-JsonFile -PathValue $templateFullPath -Name "report template"
$templateSections = Get-SectionMap -Sections $template.sections -Name "report template"

$contractIds = @($contractSections.Keys | Sort-Object)
$templateIds = @($templateSections.Keys | Sort-Object)
if (($contractIds -join "|") -ne ($templateIds -join "|")) {
    throw "Admin VPS smoke sections contract mismatch between manifest and report template."
}

foreach ($id in $contractIds) {
    if ($templateSections[$id] -ne $contractSections[$id]) {
        throw "Admin VPS smoke sections contract route mismatch for section $id."
    }
}

$reportValidator = Read-TextFile -PathValue $reportValidatorFullPath -Name "report validator"
$browserSmokeSpec = Read-TextFile -PathValue $browserSmokeSpecFullPath -Name "browser smoke spec"
$allScreensSpec = Read-TextFile -PathValue $allScreensSpecFullPath -Name "all-screens spec"
$guide = Read-TextFile -PathValue $guideFullPath -Name "operator guide"

Assert-Contains -Text $browserSmokeSpec -Needle "admin-vps-smoke-sections.json" -Message "Admin VPS smoke sections contract browser smoke spec must read admin-vps-smoke-sections.json."
Assert-Contains -Text $browserSmokeSpec -Needle "route: section.route" -Message "Admin VPS smoke sections contract browser smoke spec must write routes from manifest."
Assert-Contains -Text $guide -Needle "validate-admin-vps-smoke-sections-contract.ps1" -Message "Admin VPS smoke sections contract guide must document validator command."

foreach ($id in $contractIds) {
    Assert-Contains -Text $reportValidator -Needle "`"$id`"" -Message "Admin VPS smoke sections contract report validator is missing section: $id"
    Assert-ContainsAny -Text $browserSmokeSpec -Needles @("'$id'", "$id`:") -Message "Admin VPS smoke sections contract browser smoke spec is missing section label: $id"
    Assert-Contains -Text $allScreensSpec -Needle "'$id'" -Message "Admin VPS smoke sections contract all-screens spec is missing section: $id"
}

$summary = [ordered]@{
    contractId = $contract.contractId
    sections = $contractIds.Count
    template = $TemplatePath
    browserSmokeSpec = $BrowserSmokeSpecPath
    allScreensSpec = $AllScreensSpecPath
}

Write-Host "admin vps smoke sections contract valid $($summary | ConvertTo-Json -Compress)"
