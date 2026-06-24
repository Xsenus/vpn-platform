param(
    [Parameter(Mandatory = $true)][string]$Path,
    [string]$OutputPath = $Path
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
    throw "Production env file was not found: $Path"
}

$requiredProductionValues = [ordered]@{
    "ASPNETCORE_ENVIRONMENT" = "Production"
    "AdminBootstrap__Enabled" = "false"
    "AdminBootstrap__Password" = ""
    "AdminBootstrap__ResetExistingPassword" = "false"
    "Database__ApplyMigrationsOnStartup" = "false"
    "Database__SeedDemoData" = "false"
    "Swagger__Enabled" = "false"
}

$lines = [System.Collections.Generic.List[string]]::new()
$lines.AddRange([System.IO.File]::ReadAllLines((Resolve-Path -LiteralPath $Path)))
$seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$normalized = [System.Collections.Generic.List[string]]::new()

foreach ($line in $lines) {
    if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith("#") -or -not $line.Contains("=")) {
        $normalized.Add($line)
        continue
    }

    $key = $line.Substring(0, $line.IndexOf("=")).Trim()
    if ($requiredProductionValues.Contains($key)) {
        $normalized.Add("$key=$($requiredProductionValues[$key])")
        [void]$seen.Add($key)
        continue
    }

    $normalized.Add($line)
}

foreach ($key in $requiredProductionValues.Keys) {
    if (-not $seen.Contains($key)) {
        $normalized.Add("$key=$($requiredProductionValues[$key])")
    }
}

$resolvedOutputPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
} else {
    Join-Path (Get-Location) $OutputPath
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllLines($resolvedOutputPath, $normalized, $utf8NoBom)

Write-Host "Production env normalized for deploy. Forced keys: $($requiredProductionValues.Keys -join ', ')"
