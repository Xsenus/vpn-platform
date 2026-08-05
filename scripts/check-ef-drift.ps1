param(
    [string]$MigrationName = "__ModelDriftCheck"
)

$ErrorActionPreference = "Stop"

function Fail([string]$Message) {
    Write-Error "[FAIL] $Message"
    exit 1
}

function Ok([string]$Message) {
    Write-Host "[OK] $Message"
}

$rootDir = Split-Path -Parent $PSScriptRoot
$backendDir = Join-Path $rootDir "backend"
$migrationsRel = "backend/src/VpnPlatform.Infrastructure/Persistence/Migrations"
$migrationsDir = Join-Path $rootDir $migrationsRel
$snapshotRel = "$migrationsRel/ApplicationDbContextModelSnapshot.cs"
$snapshotFile = Join-Path $rootDir $snapshotRel

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Fail "dotnet CLI is required for EF drift check."
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Fail "git is required because this check verifies that EF does not change migration files."
}

Push-Location $rootDir
try {
    & git rev-parse --is-inside-work-tree | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Fail "EF drift check must run inside a git work tree so migration changes can be detected."
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path -LiteralPath $migrationsDir -PathType Container)) {
    Fail "EF migrations directory is missing: $migrationsRel"
}

$snapshotWasClean = $false
$snapshotOriginalBytes = $null
if (Test-Path -LiteralPath $snapshotFile -PathType Leaf) {
    Push-Location $rootDir
    try {
        & git diff --quiet -- $snapshotRel
        $snapshotWasClean = $LASTEXITCODE -eq 0
    }
    finally {
        Pop-Location
    }

    if ($snapshotWasClean) {
        $snapshotOriginalBytes = [System.IO.File]::ReadAllBytes($snapshotFile)
    }
}

function Cleanup {
    Get-ChildItem -LiteralPath $migrationsDir -Filter "*$MigrationName*.cs" -File -ErrorAction SilentlyContinue |
        Remove-Item -Force -ErrorAction SilentlyContinue

    if ($snapshotWasClean -and $null -ne $snapshotOriginalBytes) {
        [System.IO.File]::WriteAllBytes($snapshotFile, $snapshotOriginalBytes)
    }
}

$env:ASPNETCORE_ENVIRONMENT = if ($env:ASPNETCORE_ENVIRONMENT) { $env:ASPNETCORE_ENVIRONMENT } else { "Development" }
$env:ConnectionStrings__DefaultConnection = if ($env:ConnectionStrings__DefaultConnection) { $env:ConnectionStrings__DefaultConnection } else { "Host=localhost;Port=5432;Database=vpnplatform_drift;Username=vpnplatform;Password=vpnplatform" }
$env:Jwt__Issuer = if ($env:Jwt__Issuer) { $env:Jwt__Issuer } else { "vpn-platform" }
$env:Jwt__Audience = if ($env:Jwt__Audience) { $env:Jwt__Audience } else { "vpn-platform" }
$env:Jwt__SigningKey = if ($env:Jwt__SigningKey) { $env:Jwt__SigningKey } else { "ef-drift-signing-key-00000000000000000000000000" }
$env:Security__SecretEncryptionKey = if ($env:Security__SecretEncryptionKey) { $env:Security__SecretEncryptionKey } else { "ef-drift-secret-encryption-key-00000000000000000000" }
$env:Database__ApplyMigrationsOnStartup = "false"
$env:Database__SeedDemoData = "false"
$env:AdminBootstrap__Enabled = "false"
$env:Auth__RefreshTokenDays = "30"
$env:Auth__PasswordReset__ExpiryMinutes = "30"
$env:Auth__PasswordReset__ReturnTokenForValidation = "false"
$env:Email__Mode = "Disabled"
$env:Provisioning__LiveExecutionEnabled = "false"
$env:Provisioning__AllowLiveDeploy = "false"
$env:TelegramBot__Enabled = "false"
$env:TelegramBot__BotToken = ""
$env:TelegramBot__WebhookUrl = ""
$env:TelegramBot__SecretToken = ""
$env:Payments__YooMoney__Mode = "Disabled"
$env:Payments__YooKassa__Mode = "Disabled"
$env:Payments__RoboKassa__Mode = "Disabled"
$env:Payments__TelegramStars__Mode = "Disabled"
$env:Payments__CloudPayments__Mode = "Disabled"
$env:Payments__TBankAcquiring__Mode = "Disabled"
$env:Payments__Prodamus__Mode = "Disabled"
$env:Payments__Stripe__Mode = "Disabled"
$env:Payments__PayPal__Mode = "Disabled"
$env:Vpn__X3Ui__Mode = "Sandbox"
$env:X3UI_BASE_URL = ""
$env:X3UI_USERNAME = ""
$env:X3UI_PASSWORD = ""

try {
    Push-Location $backendDir
    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        Fail "dotnet tool restore failed."
    }

    & dotnet ef migrations has-pending-model-changes `
        --project src/VpnPlatform.Infrastructure `
        --startup-project src/VpnPlatform.Api `
        --context ApplicationDbContext
    $pendingStatus = $LASTEXITCODE

    if ($pendingStatus -eq 0) {
        Pop-Location
        Push-Location $rootDir
        & git diff --quiet -- $migrationsRel
        if ($LASTEXITCODE -eq 0) {
            Ok "EF model has no pending migration changes."
            exit 0
        }

        & git diff -- $migrationsRel
        Fail "EF changed migration files even though pending-model-changes reported clean."
    }

    & dotnet ef migrations add $MigrationName `
        --project src/VpnPlatform.Infrastructure `
        --startup-project src/VpnPlatform.Api `
        --context ApplicationDbContext `
        --output-dir Persistence/Migrations
    if ($LASTEXITCODE -ne 0) {
        Fail "EF diagnostic migration generation failed."
    }

    $driftFile = Get-ChildItem -LiteralPath $migrationsDir -Filter "*$MigrationName.cs" -File |
        Select-Object -First 1
    if ($null -eq $driftFile) {
        Fail "EF did not generate a drift migration file, but pending-model-changes returned non-zero."
    }

    $driftText = [System.IO.File]::ReadAllText($driftFile.FullName, [System.Text.Encoding]::UTF8)
    if ($driftText.Contains("migrationBuilder.")) {
        Write-Error "[FAIL] EF model drift detected. Review generated migration before removing it: $($driftFile.FullName)"
        Write-Error $driftText
        exit 1
    }

    Cleanup
    Pop-Location
    Push-Location $rootDir
    & git diff --quiet -- $migrationsRel
    if ($LASTEXITCODE -eq 0) {
        Ok "EF generated an empty drift migration and migration files are clean."
        exit 0
    }

    & git diff -- $migrationsRel
    Fail "EF drift check left migration changes after cleanup."
}
finally {
    Cleanup
    while ((Get-Location).Path -ne $rootDir -and (Get-Location).Path.StartsWith($rootDir, [System.StringComparison]::OrdinalIgnoreCase)) {
        Pop-Location
    }
}
