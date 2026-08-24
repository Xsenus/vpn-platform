param(
    [string]$OutputDirectory = "tmp/normalize-production-env-test"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$scriptPath = Join-Path $repoRoot "scripts/normalize-production-env.ps1"
$defaultOutputDirectory = "tmp/normalize-production-env-test"
$usingDefaultOutputDirectory = -not $PSBoundParameters.ContainsKey("OutputDirectory") -or [string]::Equals($OutputDirectory, $defaultOutputDirectory, [System.StringComparison]::OrdinalIgnoreCase)

function Resolve-WorkspacePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Assert-InWorkspace {
    param([Parameter(Mandatory = $true)][string]$Path)

    $rootFullPath = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $targetFullPath = [System.IO.Path]::GetFullPath($Path).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    if (-not $targetFullPath.StartsWith($rootFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to write outside workspace: $targetFullPath"
    }
}

$testRoot = Resolve-WorkspacePath $OutputDirectory
Assert-InWorkspace -Path $testRoot

try {
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
        "Email__Mode=Smtp",
        "Email__Host=smtp.test.invalid",
        "Email__Port=587",
        "Email__FromAddress=no-reply@test.invalid",
        "Email__Username=smtp-test-user",
        "Email__Password=smtp-test-password",
        "ConnectionStrings__DefaultConnection=Host=127.0.0.1;Database=vpnplatform;Username=vpnplatform;Password=db-secret",
        "Jwt__SigningKey=jwt-secret"
    )

    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllLines($inputPath, $inputLines, $utf8NoBom)

    & $scriptPath -Path $inputPath -OutputPath $outputPath -RequireStartupReady

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
        "Email__Mode=Smtp",
        "Email__Host=smtp.test.invalid",
        "Email__Port=587",
        "Email__FromAddress=no-reply@test.invalid",
        "Email__Username=smtp-test-user",
        "Email__Password=smtp-test-password",
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

    $invalidInputPath = Join-Path $testRoot "production-missing-smtp.env"
    $invalidOutputPath = Join-Path $testRoot "normalized-missing-smtp.env"
    $smtpValueThatMustNotLeak = "smtp-value-that-must-not-leak"
    [System.IO.File]::WriteAllLines($invalidInputPath, @(
        "ASPNETCORE_ENVIRONMENT=Production",
        "Email__Mode=Disabled",
        "Email__Username=smtp-test-user",
        "Email__Password=$smtpValueThatMustNotLeak"
    ), $utf8NoBom)

    $failureMessage = ""
    try {
        & $scriptPath -Path $invalidInputPath -OutputPath $invalidOutputPath -RequireStartupReady | Out-Null
        throw "Expected missing SMTP settings to fail production startup preflight."
    }
    catch {
        if ($_.Exception.Message.IndexOf("Expected missing SMTP settings", [System.StringComparison]::Ordinal) -ge 0) {
            throw
        }

        $failureMessage = $_.Exception.Message
    }

    foreach ($expectedFailure in @("Email__Mode", "Email__Host", "Email__Port", "Email__FromAddress")) {
        if ($failureMessage.IndexOf($expectedFailure, [System.StringComparison]::Ordinal) -lt 0) {
            throw "Missing SMTP settings failure did not mention $expectedFailure."
        }
    }

    if ($failureMessage.IndexOf($smtpValueThatMustNotLeak, [System.StringComparison]::Ordinal) -ge 0) {
        throw "Production startup preflight exposed an SMTP value."
    }

    Write-Host "normalize production env regression ok"
}
finally {
    if ($usingDefaultOutputDirectory -and (Test-Path -LiteralPath $testRoot)) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }

    if ($usingDefaultOutputDirectory) {
        $tmpDirectory = Resolve-WorkspacePath "tmp"
        if ((Test-Path -LiteralPath $tmpDirectory) -and -not (Get-ChildItem -LiteralPath $tmpDirectory -Force)) {
            Remove-Item -LiteralPath $tmpDirectory -Force
        }
    }
}
