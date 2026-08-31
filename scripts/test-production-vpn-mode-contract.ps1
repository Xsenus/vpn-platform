param()

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$configureScript = Get-Content -LiteralPath (Join-Path $repoRoot "scripts/configure-production-vpn-mode-remote.sh") -Raw -Encoding UTF8
$deployWorkflow = Get-Content -LiteralPath (Join-Path $repoRoot ".github/workflows/deploy-vps.yml") -Raw -Encoding UTF8
$smokeWorkflow = Get-Content -LiteralPath (Join-Path $repoRoot ".github/workflows/production-vpn-smoke.yml") -Raw -Encoding UTF8

foreach ($required in @(
    'Vpn__X3Ui__Mode=Production',
    'production.override.env',
    'chmod 600',
    '/health/ready',
    'systemctl restart vpn-platform-api'
)) {
    if (-not $configureScript.Contains($required, [StringComparison]::Ordinal)) {
        throw "Production VPN mode configurator is missing contract marker: $required"
    }
}

$sharedIndex = $deployWorkflow.IndexOf('EnvironmentFile=-$APP_DIR/shared/.env', [StringComparison]::Ordinal)
$overrideIndex = $deployWorkflow.IndexOf('EnvironmentFile=-$APP_DIR/shared/production.override.env', [StringComparison]::Ordinal)
if ($sharedIndex -lt 0 -or $overrideIndex -le $sharedIndex) {
    throw "Production override EnvironmentFile must be loaded after the shared production env."
}

foreach ($required in @(
    'enable_production_mode:',
    'Persist production VPN mode',
    'configure-production-vpn-mode-remote.sh',
    'Vpn__X3Ui__Mode=Production'
)) {
    if (-not $smokeWorkflow.Contains($required, [StringComparison]::Ordinal)) {
        throw "Production VPN smoke workflow is missing contract marker: $required"
    }
}

Write-Host "production VPN mode contract passed"
