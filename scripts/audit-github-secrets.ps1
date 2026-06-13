param(
    [string]$ConfigPath = ".github/github-secrets.audit.json",
    [string]$Repository,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Resolve-RepositoryRoot {
    $directory = Get-Item -LiteralPath (Get-Location)
    while ($null -ne $directory) {
        if ((Test-Path -LiteralPath (Join-Path $directory.FullName ".git")) -and
            (Test-Path -LiteralPath (Join-Path $directory.FullName ".github"))) {
            return $directory.FullName
        }

        $directory = $directory.Parent
    }

    throw "Repository root was not found."
}

function Get-GitHubToken {
    foreach ($name in @("GITHUB_TOKEN", "GH_TOKEN", "GITHUB_PAT")) {
        $value = [Environment]::GetEnvironmentVariable($name, "Process")
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            return $value
        }
    }

    throw "GitHub token is required. Set GITHUB_TOKEN or GH_TOKEN before running without -DryRun."
}

function Invoke-GitHubJson {
    param(
        [Parameter(Mandatory = $true)][string]$Uri,
        [Parameter(Mandatory = $true)][hashtable]$Headers
    )

    Invoke-RestMethod -Method Get -Uri $Uri -Headers $Headers -ContentType "application/json"
}

$root = Resolve-RepositoryRoot
$resolvedConfig = if ([System.IO.Path]::IsPathRooted($ConfigPath)) {
    $ConfigPath
} else {
    Join-Path $root $ConfigPath
}

if (-not (Test-Path -LiteralPath $resolvedConfig)) {
    throw "GitHub secrets audit config was not found: $resolvedConfig"
}

$config = Get-Content -LiteralPath $resolvedConfig -Raw -Encoding UTF8 | ConvertFrom-Json
$targetRepository = if ([string]::IsNullOrWhiteSpace($Repository)) { $config.repository } else { $Repository }
if ([string]::IsNullOrWhiteSpace($targetRepository) -or $targetRepository -notmatch "^[^/]+/[^/]+$") {
    throw "Repository must use owner/name format."
}

$requiredSecrets = @($config.requiredSecrets | ForEach-Object { $_.name } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
$optionalSecrets = @($config.optionalSecrets | ForEach-Object { $_.name } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
if ($requiredSecrets.Count -eq 0) {
    throw "At least one required secret must be configured."
}

$workflowPath = Join-Path $root ".github/workflows/deploy-vps.yml"
$workflow = Get-Content -LiteralPath $workflowPath -Raw -Encoding UTF8
foreach ($secretName in $requiredSecrets + $optionalSecrets) {
    if ($workflow -notmatch [Regex]::Escape("secrets.$secretName")) {
        throw "Secret '$secretName' is listed in audit config but is not referenced by deploy-vps workflow."
    }
}

Write-Host "Repository: $targetRepository"
Write-Host "Required GitHub secrets:"
foreach ($secretName in $requiredSecrets) {
    Write-Host "  - $secretName"
}

Write-Host "Optional GitHub secrets:"
foreach ($secretName in $optionalSecrets) {
    Write-Host "  - $secretName"
}

if ($DryRun) {
    Write-Host "Dry run: local config and workflow references were checked. GitHub secrets API was not called."
    exit 0
}

$token = Get-GitHubToken
$headers = @{
    Accept                 = "application/vnd.github+json"
    Authorization          = "Bearer $token"
    "X-GitHub-Api-Version" = "2022-11-28"
    "User-Agent"           = "vpn-platform-secrets-audit"
}

$allSecretNames = New-Object System.Collections.Generic.List[string]
$page = 1
do {
    $uri = "https://api.github.com/repos/$targetRepository/actions/secrets?per_page=100&page=$page"
    $response = Invoke-GitHubJson -Uri $uri -Headers $headers
    foreach ($secret in @($response.secrets)) {
        if (-not [string]::IsNullOrWhiteSpace($secret.name)) {
            $allSecretNames.Add([string]$secret.name)
        }
    }

    $page++
} while (@($response.secrets).Count -eq 100)

$present = @($allSecretNames | Sort-Object -Unique)
$missingRequired = @($requiredSecrets | Where-Object { $present -notcontains $_ })
$missingOptional = @($optionalSecrets | Where-Object { $present -notcontains $_ })
$unknown = @($present | Where-Object { ($requiredSecrets + $optionalSecrets) -notcontains $_ })

Write-Host "GitHub returned $($present.Count) repository secret name(s). Values were not requested and are not available through this API."
Write-Host "Present required secrets:"
foreach ($secretName in @($requiredSecrets | Where-Object { $present -contains $_ })) {
    Write-Host "  - $secretName"
}

if ($missingRequired.Count -gt 0) {
    Write-Host "Missing required secrets:"
    foreach ($secretName in $missingRequired) {
        Write-Host "  - $secretName"
    }
}

if ($missingOptional.Count -gt 0) {
    Write-Host "Missing optional secrets:"
    foreach ($secretName in $missingOptional) {
        Write-Host "  - $secretName"
    }
}

if ($unknown.Count -gt 0) {
    Write-Host "Additional repository secrets not described in audit config:"
    foreach ($secretName in $unknown) {
        Write-Host "  - $secretName"
    }
}

if ($missingRequired.Count -gt 0) {
    throw "GitHub secrets audit failed: missing required secret name(s): $($missingRequired -join ', ')"
}

Write-Host "GitHub secrets audit passed."
