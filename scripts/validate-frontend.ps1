param(
    [switch]$SkipInstall,
    [switch]$SkipAudit
)

$ErrorActionPreference = "Stop"

function Require-Command {
    param([string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command is missing: $Name"
    }
}

function Run-Step {
    param(
        [string]$Title,
        [scriptblock]$Command
    )

    Write-Host $Title
    & $Command
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

function Test-FrontendLockfileSafety {
    param([string]$RootDir)

    $lockfile = Join-Path $RootDir "frontend/package-lock.json"
    $npmrc = Join-Path $RootDir "frontend/.npmrc"
    if (-not (Test-Path -LiteralPath $lockfile)) {
        throw "frontend/package-lock.json is missing. Run npm install/npm ci in frontend and commit the lockfile."
    }

    $forbiddenPattern = "(npm\.openai|registry\.openai|packages\.ace-research\.openai\.org|npm\.pkg\.github\.com|_authToken|always-auth|//[^\s]+:_auth|//[^\s]+:_password|_auth=)"
    $files = @($lockfile)
    if (Test-Path -LiteralPath $npmrc) {
        $files += $npmrc
    }

    foreach ($file in $files) {
        $content = Get-Content -LiteralPath $file -Raw
        if ($content -match $forbiddenPattern) {
            throw "Frontend npm lock/config contains an internal registry or private auth token: $file"
        }
    }

    $lockContent = Get-Content -LiteralPath $lockfile -Raw
    if ($lockContent -notmatch "https://registry\.npmjs\.org/") {
        Write-Warning "frontend/package-lock.json does not explicitly contain https://registry.npmjs.org/."
    }

    Write-Host "[OK] frontend npm lock/config does not contain internal registries or private auth tokens."
}

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$frontend = Join-Path $root "frontend"

Require-Command "node"
Require-Command "npm"

$env:CI = if ($env:CI) { $env:CI } else { "true" }

Run-Step "[1/6] Node/npm environment" {
    node --version
    npm --version
    npm config get registry
}

Run-Step "[2/6] Frontend npm lock/config safety check" {
    Test-FrontendLockfileSafety -RootDir $root
}

Push-Location $frontend
try {
    if ($SkipInstall) {
        Write-Host "[3/6] Frontend npm ci skipped by -SkipInstall"
    }
    else {
        Run-Step "[3/6] Frontend npm ci" {
            npm ci
        }
    }

    Run-Step "[4/6] Frontend typecheck" {
        npm run typecheck
    }

    Run-Step "[5/6] Frontend build" {
        npm run build
    }

    Run-Step "[6/6] Frontend unit tests" {
        npm run test
    }

    if ($SkipAudit) {
        Write-Host "[audit] npm audit skipped by -SkipAudit"
    }
    else {
        Run-Step "[audit] Frontend dependency audit, high severity" {
            npm audit --audit-level=high
        }
    }
}
finally {
    Pop-Location
}

Write-Host "[OK] frontend validation gate completed."
