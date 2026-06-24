param(
    [string]$OutputDirectory = "tmp/normalize-production-env-test"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$scriptPath = Join-Path $repoRoot "scripts/normalize-production-env.ps1"
$testRoot = Join-Path $repoRoot $OutputDirectory

if (Test-Path -LiteralPath $testRoot) {
    Remove-Item -LiteralPath $testRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $testRoot | Out-Null

$inputPath = Join-Path $testRoot "production.env"
$outputPath = Join-Path $testRoot "normalized.env"

$inputLines = @(
    "ASPNETCORE_ENVIRONMENT=Local",
    "AdminBootstrap__Enabled=true",
    "AdminBootstrap__Password=temporary-secret-that-must-not-stay",
    "Database__ApplyMigrationsOnStartup=true",
    "Database__SeedDemoData=true",
    "Swagger__Enabled=true",
    "ConnectionStrings__DefaultConnection=Host=127.0.0.1;Database=vpnplatform;Username=vpnplatform;Password=db-secret",
    "Jwt__SigningKey=jwt-secret"
)

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllLines($inputPath, $inputLines, $utf8NoBom)

& $scriptPath -Path $inputPath -OutputPath $outputPath

$outputBytes = [System.IO.File]::ReadAllBytes($outputPath)
if ($outputBytes.Length -ge 3 -and $outputBytes[0] -eq 0xEF -and $outputBytes[1] -eq 0xBB -and $outputBytes[2] -eq 0xBF) {
    throw "Normalized env must be written as UTF-8 without BOM."
}

$content = Get-Content -LiteralPath $outputPath -Raw -Encoding UTF8

foreach ($expected in @(
    "ASPNETCORE_ENVIRONMENT=Production",
    "AdminBootstrap__Enabled=false",
    "AdminBootstrap__Password=",
    "AdminBootstrap__ResetExistingPassword=false",
    "Database__ApplyMigrationsOnStartup=false",
    "Database__SeedDemoData=false",
    "Swagger__Enabled=false",
    "ConnectionStrings__DefaultConnection=Host=127.0.0.1;Database=vpnplatform;Username=vpnplatform;Password=db-secret",
    "Jwt__SigningKey=jwt-secret"
)) {
    if (-not $content.Contains($expected)) {
        throw "Normalized env does not contain expected line: $expected"
    }
}

foreach ($forbidden in @(
    "ASPNETCORE_ENVIRONMENT=Local",
    "AdminBootstrap__Enabled=true",
    "temporary-secret-that-must-not-stay",
    "Database__ApplyMigrationsOnStartup=true",
    "Database__SeedDemoData=true",
    "Swagger__Enabled=true"
)) {
    if ($content.Contains($forbidden)) {
        throw "Normalized env still contains forbidden line/value: $forbidden"
    }
}

Write-Host "normalize production env regression ok"
