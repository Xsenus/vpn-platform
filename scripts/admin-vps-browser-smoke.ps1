param(
    [Parameter(Mandatory = $true)]
    [string]$ApiBaseUrl,

    [Parameter(Mandatory = $true)]
    [string]$AdminWebUrl,

    [Parameter(Mandatory = $true)]
    [string]$AdminEmail,

    [string]$OutputPath = "tmp/admin-vps-smoke-report.json",
    [string]$EnvironmentName = "staging",
    [string]$Operator = "",
    [string]$ReleaseId = "",
    [string]$FrontendPath = "frontend",
    [switch]$AccountBootstrapChecked,
    [switch]$RequireAllPassed
)

$ErrorActionPreference = "Stop"

function Require-Value {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [AllowEmptyString()][string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "$Name is required."
    }
}

function Assert-HttpUrl {
    param(
        [string]$Value,
        [string]$Name
    )

    $parsed = $null
    $isInvalid = [string]::IsNullOrWhiteSpace($Value) -or -not [Uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$parsed) -or ($parsed.Scheme -ne "http" -and $parsed.Scheme -ne "https")
    if ($isInvalid) {
        throw "$Name must be an absolute http or https URL."
    }
}

function Set-ProcessEnv {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [AllowEmptyString()][string]$Value
    )

    [System.Environment]::SetEnvironmentVariable($Name, $Value, "Process")
}

Require-Value "Admin email" $AdminEmail
Require-Value "ADMIN_VPS_SMOKE_ADMIN_PASSWORD environment variable" $env:ADMIN_VPS_SMOKE_ADMIN_PASSWORD
Assert-HttpUrl -Value $ApiBaseUrl -Name "ApiBaseUrl"
Assert-HttpUrl -Value $AdminWebUrl -Name "AdminWebUrl"

$repoRoot = Split-Path -Parent $PSScriptRoot
$frontendFullPath = if ([System.IO.Path]::IsPathRooted($FrontendPath)) { $FrontendPath } else { Join-Path $repoRoot $FrontendPath }
if (-not (Test-Path -LiteralPath $frontendFullPath -PathType Container)) {
    throw "Frontend directory was not found: $frontendFullPath"
}

$reportFullPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputPath))

Set-ProcessEnv "ADMIN_VPS_SMOKE_API_BASE_URL" $ApiBaseUrl
Set-ProcessEnv "ADMIN_VPS_SMOKE_ADMIN_WEB_URL" $AdminWebUrl
Set-ProcessEnv "ADMIN_VPS_SMOKE_ADMIN_EMAIL" $AdminEmail
Set-ProcessEnv "ADMIN_VPS_SMOKE_REPORT_PATH" $reportFullPath
Set-ProcessEnv "ADMIN_VPS_SMOKE_ENVIRONMENT" $EnvironmentName
Set-ProcessEnv "ADMIN_VPS_SMOKE_OPERATOR" $Operator
Set-ProcessEnv "ADMIN_VPS_SMOKE_RELEASE_ID" $ReleaseId
Set-ProcessEnv "ADMIN_VPS_SMOKE_ACCOUNT_BOOTSTRAP_CHECKED" $(if ($AccountBootstrapChecked) { "true" } else { "false" })

Write-Host "Admin VPS browser smoke is ready to run."
Write-Host "API base URL: $ApiBaseUrl"
Write-Host "Admin web URL: $AdminWebUrl"
Write-Host "Admin email: $AdminEmail"
Write-Host "Password: [hidden]"
Write-Host "Report path: $reportFullPath"
Write-Host "Account bootstrap checked: $AccountBootstrapChecked"

Push-Location $frontendFullPath
try {
    npm run e2e:admin-vps-smoke
}
finally {
    Pop-Location
}

$validator = Join-Path $repoRoot "scripts/validate-admin-vps-smoke-report.ps1"
if ($RequireAllPassed) {
    & $validator -ReportPath $reportFullPath -RequireAllPassed
} else {
    & $validator -ReportPath $reportFullPath
}
