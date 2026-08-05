param(
    [string]$Configuration = $(if ($env:CONFIGURATION) { $env:CONFIGURATION } else { "Release" }),
    [string]$TestResultsDir = $(if ($env:TEST_RESULTS_DIR) { $env:TEST_RESULTS_DIR } else { "" }),
    [switch]$SkipEfDrift
)

$ErrorActionPreference = "Stop"
$rootDir = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($TestResultsDir)) {
    $TestResultsDir = Join-Path $rootDir "backend/TestResults"
}

function Require-Command {
    param([Parameter(Mandatory = $true)][string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "[FAIL] Required command is missing: $Name"
    }
}

function Run-Step {
    param(
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)][scriptblock]$Command
    )

    Write-Host $Title
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "[FAIL] Step failed: $Title"
    }
}

function Get-BashCommand {
    $candidates = @(
        "C:\Program Files\Git\bin\bash.exe",
        "C:\Program Files\Git\usr\bin\bash.exe",
        "C:\Program Files (x86)\Git\bin\bash.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return $candidate
        }
    }

    $command = Get-Command bash -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Path
    }

    return $null
}

Push-Location $rootDir
try {
    Require-Command dotnet
    Require-Command git

    $env:ASPNETCORE_ENVIRONMENT = if ($env:ASPNETCORE_ENVIRONMENT) { $env:ASPNETCORE_ENVIRONMENT } else { "Development" }
    $env:ConnectionStrings__DefaultConnection = if ($env:ConnectionStrings__DefaultConnection) { $env:ConnectionStrings__DefaultConnection } else { "Host=localhost;Port=5432;Database=vpnplatform_validation;Username=vpnplatform;Password=vpnplatform" }
    $env:Jwt__Issuer = if ($env:Jwt__Issuer) { $env:Jwt__Issuer } else { "vpn-platform" }
    $env:Jwt__Audience = if ($env:Jwt__Audience) { $env:Jwt__Audience } else { "vpn-platform" }
    $env:Jwt__SigningKey = if ($env:Jwt__SigningKey) { $env:Jwt__SigningKey } else { "local-validation-signing-key-0000000000000000000000" }
    $env:Security__SecretEncryptionKey = if ($env:Security__SecretEncryptionKey) { $env:Security__SecretEncryptionKey } else { "local-validation-secret-encryption-key-000000000000000000" }
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

    if (Test-Path -LiteralPath (Join-Path $rootDir "global.json")) {
        Write-Host "[info] global.json: $((Get-Content -LiteralPath (Join-Path $rootDir "global.json") -Raw).Trim())"
    }

    Run-Step "[1/8] Validation safety defaults" {
        $bash = Get-BashCommand
        if ($bash) {
            & $bash ./scripts/check-validation-safety.sh
        } else {
            Write-Host "[skip] bash not found; safety defaults are covered by backend guard tests and CI."
        }
    }

    Run-Step "[2/8] Repository secret scan" {
        & powershell -ExecutionPolicy Bypass -File scripts/scan-secrets.ps1
    }

    Run-Step "[3/8] .NET environment" {
        & dotnet --info
    }

    Run-Step "[4/8] Restore backend solution" {
        & dotnet restore backend/VpnPlatform.sln
    }

    Run-Step "[5/8] Build backend solution" {
        & dotnet build backend/VpnPlatform.sln --configuration $Configuration --no-restore
    }

    New-Item -ItemType Directory -Force -Path $TestResultsDir | Out-Null
    Run-Step "[6/8] Run backend tests" {
        & dotnet test backend/VpnPlatform.sln `
            --configuration $Configuration `
            --no-build `
            --logger "trx;LogFileName=test-results.trx" `
            --results-directory $TestResultsDir
    }

    Run-Step "[7/8] Restore dotnet tools and list EF migrations" {
        Push-Location (Join-Path $rootDir "backend")
        try {
            & dotnet tool restore
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

            & dotnet ef migrations list `
                --project src/VpnPlatform.Infrastructure `
                --startup-project src/VpnPlatform.Api `
                --context ApplicationDbContext `
                --no-connect
        }
        finally {
            Pop-Location
        }
    }

    if ($SkipEfDrift) {
        Write-Host "[8/8] EF model drift check skipped by -SkipEfDrift."
    } else {
        Run-Step "[8/8] EF model drift check" {
            & powershell -ExecutionPolicy Bypass -File scripts/check-ef-drift.ps1
        }
    }

    Write-Host "[OK] backend validation gate completed. Results: $TestResultsDir"
}
finally {
    Pop-Location
}
