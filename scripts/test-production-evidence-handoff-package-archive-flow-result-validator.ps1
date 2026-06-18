param(
    [Parameter(Mandatory = $true)]
    [string]$ResultJsonPath,

    [string]$ResultMarkdownPath = "",
    [switch]$RequireProductionReady,
    [switch]$WriteJson
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$RelativePath)

    $repoRoot = Split-Path -Parent $PSScriptRoot
    return Join-Path $repoRoot $RelativePath
}

function Write-Utf8NoBomFile {
    param(
        [string]$PathValue,
        [string]$Content
    )

    [System.IO.File]::WriteAllText($PathValue, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Invoke-ResultValidator {
    param(
        [string]$JsonPath,
        [string]$MarkdownPath = ""
    )

    $validatorArgs = @{
        ResultJsonPath = $JsonPath
        WriteJson = $true
    }

    if (-not [string]::IsNullOrWhiteSpace($MarkdownPath)) {
        $validatorArgs.ResultMarkdownPath = $MarkdownPath
    }

    if ($RequireProductionReady) {
        $validatorArgs.RequireProductionReady = $true
    }

    return & (Resolve-RepoPath "scripts/validate-production-evidence-handoff-package-archive-flow-result.ps1") @validatorArgs
}

function Assert-FailsWith {
    param(
        [scriptblock]$Action,
        [string]$ExpectedMessage
    )

    try {
        & $Action | Out-Null
    }
    catch {
        $message = $_.Exception.Message
        if ($message -notlike "*$ExpectedMessage*") {
            throw "Expected failure containing '$ExpectedMessage', actual: $message"
        }

        return $message
    }

    throw "Expected command to fail with '$ExpectedMessage'."
}

function Copy-ResultJson {
    param(
        [object]$Source,
        [string]$DestinationPath
    )

    Write-Utf8NoBomFile -PathValue $DestinationPath -Content ($Source | ConvertTo-Json -Depth 10)
    return $DestinationPath
}

if ([string]::IsNullOrWhiteSpace($ResultJsonPath) -or -not (Test-Path -LiteralPath $ResultJsonPath -PathType Leaf)) {
    throw "Production evidence handoff package archive flow result JSON was not found: $ResultJsonPath"
}

$resultJsonFullPath = (Resolve-Path -LiteralPath $ResultJsonPath).Path
$result = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ([string]::IsNullOrWhiteSpace($ResultMarkdownPath)) {
    $ResultMarkdownPath = [string]$result.resultMarkdownPath
}

if ([string]::IsNullOrWhiteSpace($ResultMarkdownPath) -or -not (Test-Path -LiteralPath $ResultMarkdownPath -PathType Leaf)) {
    throw "Production evidence handoff package archive flow result Markdown was not found: $ResultMarkdownPath"
}

$resultMarkdownFullPath = (Resolve-Path -LiteralPath $ResultMarkdownPath).Path
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("vpn-platform-flow-result-validator-test-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

try {
    $validJson = Invoke-ResultValidator -JsonPath $resultJsonFullPath -MarkdownPath $resultMarkdownFullPath
    $valid = $validJson | ConvertFrom-Json

    $badStatus = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badStatus.status = "failed"
    $badStatusPath = Copy-ResultJson -Source $badStatus -DestinationPath (Join-Path $tempRoot "bad-status-result.json")
    $badStatusMessage = Assert-FailsWith -ExpectedMessage "status must be passed" -Action {
        Invoke-ResultValidator -JsonPath $badStatusPath -MarkdownPath $resultMarkdownFullPath
    }

    $badSha = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badSha.handoffPackageArchiveSha256 = "0" * 64
    if ($badSha.handoffPackageArchiveSha256 -eq [string]$result.handoffPackageArchiveSha256) {
        $badSha.handoffPackageArchiveSha256 = "1" * 64
    }
    $badShaPath = Copy-ResultJson -Source $badSha -DestinationPath (Join-Path $tempRoot "bad-sha-result.json")
    $badShaMessage = Assert-FailsWith -ExpectedMessage "SHA256 does not match" -Action {
        Invoke-ResultValidator -JsonPath $badShaPath -MarkdownPath $resultMarkdownFullPath
    }

    $missingFailure = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $missingFailure.testedFailures = @($missingFailure.testedFailures | Where-Object { [string]$_.name -ne "missing-required-entry" })
    $missingFailurePath = Copy-ResultJson -Source $missingFailure -DestinationPath (Join-Path $tempRoot "missing-failure-result.json")
    $missingFailureMessage = Assert-FailsWith -ExpectedMessage "missing regression failure" -Action {
        Invoke-ResultValidator -JsonPath $missingFailurePath -MarkdownPath $resultMarkdownFullPath
    }

    $badMarkdownPath = Join-Path $tempRoot "bad-result.md"
    $badMarkdown = (Get-Content -LiteralPath $resultMarkdownFullPath -Raw -Encoding UTF8).Replace("Tested failures", "Regression checks")
    Write-Utf8NoBomFile -PathValue $badMarkdownPath -Content $badMarkdown
    $badMarkdownMessage = Assert-FailsWith -ExpectedMessage "markdown is missing" -Action {
        Invoke-ResultValidator -JsonPath $resultJsonFullPath -MarkdownPath $badMarkdownPath
    }

    $regression = [ordered]@{
        status = "passed"
        resultJsonPath = $resultJsonFullPath
        resultMarkdownPath = $resultMarkdownFullPath
        releaseId = [string]$valid.releaseId
        packageStatus = [string]$valid.packageStatus
        productionReady = [bool]$valid.productionReady
        testedFailures = @(
            [ordered]@{ name = "bad-status"; message = $badStatusMessage },
            [ordered]@{ name = "bad-handoff-archive-sha256"; message = $badShaMessage },
            [ordered]@{ name = "missing-regression-failure"; message = $missingFailureMessage },
            [ordered]@{ name = "bad-markdown"; message = $badMarkdownMessage }
        )
    }

    if ($WriteJson) {
        Write-Output ($regression | ConvertTo-Json -Depth 8)
    }
    else {
        Write-Host "production evidence handoff package archive flow result validator regression passed $($regression | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
