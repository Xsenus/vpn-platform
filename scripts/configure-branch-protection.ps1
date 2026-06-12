param(
    [string]$ConfigPath = ".github/branch-protection.required-checks.json",
    [string]$Repository,
    [string[]]$Branches,
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

    throw "GitHub token is required. Set GITHUB_TOKEN or GH_TOKEN with admin:repo_hook/repo administration permissions before running without -DryRun."
}

function Invoke-GitHubJson {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Uri,
        [Parameter(Mandatory = $true)][hashtable]$Headers,
        [object]$Body = $null
    )

    $parameters = @{
        Method      = $Method
        Uri         = $Uri
        Headers     = $Headers
        ContentType = "application/json"
    }

    if ($null -ne $Body) {
        $parameters.Body = ($Body | ConvertTo-Json -Depth 20)
    }

    Invoke-RestMethod @parameters
}

$root = Resolve-RepositoryRoot
$resolvedConfig = if ([System.IO.Path]::IsPathRooted($ConfigPath)) {
    $ConfigPath
} else {
    Join-Path $root $ConfigPath
}

if (-not (Test-Path -LiteralPath $resolvedConfig)) {
    throw "Branch protection config was not found: $resolvedConfig"
}

$config = Get-Content -LiteralPath $resolvedConfig -Raw -Encoding UTF8 | ConvertFrom-Json
$targetRepository = if ([string]::IsNullOrWhiteSpace($Repository)) { $config.repository } else { $Repository }
if ([string]::IsNullOrWhiteSpace($targetRepository) -or $targetRepository -notmatch "^[^/]+/[^/]+$") {
    throw "Repository must use owner/name format."
}

$targetBranches = if ($Branches -and $Branches.Count -gt 0) { $Branches } else { @($config.branches) }
if (-not $targetBranches -or $targetBranches.Count -eq 0) {
    throw "At least one branch must be configured."
}

$contexts = @($config.requiredStatusChecks.contexts | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($contexts.Count -eq 0) {
    throw "At least one required status check context must be configured."
}

$body = [ordered]@{
    required_status_checks = [ordered]@{
        strict   = [bool]$config.requiredStatusChecks.strict
        contexts = $contexts
    }
    enforce_admins                  = [bool]$config.enforceAdmins
    required_pull_request_reviews   = [ordered]@{
        dismiss_stale_reviews           = [bool]$config.pullRequestReviews.dismissStaleReviews
        require_code_owner_reviews      = [bool]$config.pullRequestReviews.requireCodeOwnerReviews
        required_approving_review_count = [int]$config.pullRequestReviews.requiredApprovingReviewCount
    }
    restrictions                    = $null
    required_linear_history         = $false
    allow_force_pushes              = [bool]$config.allowForcePushes
    allow_deletions                 = [bool]$config.allowDeletions
    required_conversation_resolution = [bool]$config.requiredConversationResolution
}

Write-Host "Repository: $targetRepository"
Write-Host "Branches: $($targetBranches -join ', ')"
Write-Host "Required checks:"
foreach ($context in $contexts) {
    Write-Host "  - $context"
}

if ($DryRun) {
    Write-Host "Dry run: branch protection payload was built but not sent to GitHub."
    $body | ConvertTo-Json -Depth 20
    exit 0
}

$token = Get-GitHubToken
$headers = @{
    Accept                 = "application/vnd.github+json"
    Authorization          = "Bearer $token"
    "X-GitHub-Api-Version" = "2022-11-28"
    "User-Agent"           = "vpn-platform-branch-protection"
}

foreach ($branch in $targetBranches) {
    $encodedBranch = [System.Uri]::EscapeDataString($branch)
    $branchUri = "https://api.github.com/repos/$targetRepository/branches/$encodedBranch"
    $protectionUri = "$branchUri/protection"

    Write-Host "Checking branch '$branch'..."
    $null = Invoke-GitHubJson -Method Get -Uri $branchUri -Headers $headers

    Write-Host "Applying protection to '$branch'..."
    $null = Invoke-GitHubJson -Method Put -Uri $protectionUri -Headers $headers -Body $body

    Write-Host "Reading protection back for '$branch'..."
    $applied = Invoke-GitHubJson -Method Get -Uri $protectionUri -Headers $headers
    $appliedContexts = @($applied.required_status_checks.contexts)
    foreach ($context in $contexts) {
        if ($appliedContexts -notcontains $context) {
            throw "Required status check '$context' was not found in GitHub response for branch '$branch'."
        }
    }

    Write-Host "Branch '$branch' protection is configured."
}
