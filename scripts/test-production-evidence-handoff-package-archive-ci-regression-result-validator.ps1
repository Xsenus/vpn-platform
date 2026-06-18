param(
    [Parameter(Mandatory = $true)]
    [string]$ResultJsonPath,

    [string]$ResultMarkdownPath = "",
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

    return & (Resolve-RepoPath "scripts/validate-production-evidence-handoff-package-archive-ci-regression-result.ps1") @validatorArgs
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

    Write-Utf8NoBomFile -PathValue $DestinationPath -Content ($Source | ConvertTo-Json -Depth 12)
    return $DestinationPath
}

if ([string]::IsNullOrWhiteSpace($ResultJsonPath) -or -not (Test-Path -LiteralPath $ResultJsonPath -PathType Leaf)) {
    throw "Production evidence handoff package archive CI regression result validator JSON was not found: $ResultJsonPath"
}

$resultJsonFullPath = (Resolve-Path -LiteralPath $ResultJsonPath).Path
$result = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ([string]::IsNullOrWhiteSpace($ResultMarkdownPath)) {
    $ResultMarkdownPath = [string]$result.resultMarkdownPath
}

if ([string]::IsNullOrWhiteSpace($ResultMarkdownPath) -or -not (Test-Path -LiteralPath $ResultMarkdownPath -PathType Leaf)) {
    throw "Production evidence handoff package archive CI regression result validator Markdown was not found: $ResultMarkdownPath"
}

$resultMarkdownFullPath = (Resolve-Path -LiteralPath $ResultMarkdownPath).Path
$markdown = Get-Content -LiteralPath $resultMarkdownFullPath -Raw -Encoding UTF8

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("vpn-platform-ci-regression-result-validator-test-" + [Guid]::NewGuid().ToString("N"))
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

    $missingRelease = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $missingRelease.releaseId = ""
    $missingReleasePath = Copy-ResultJson -Source $missingRelease -DestinationPath (Join-Path $tempRoot "missing-release-result.json")
    $missingReleaseMessage = Assert-FailsWith -ExpectedMessage "releaseId is required" -Action {
        Invoke-ResultValidator -JsonPath $missingReleasePath -MarkdownPath $resultMarkdownFullPath
    }

    $missingFailure = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $missingFailure.ciSummaryValidatorRegression.testedFailures = @(
        $missingFailure.ciSummaryValidatorRegression.testedFailures |
            Where-Object { [string]$_.name -ne "missing-artifact-path" }
    )
    $missingFailurePath = Copy-ResultJson -Source $missingFailure -DestinationPath (Join-Path $tempRoot "missing-summary-failure-result.json")
    $missingFailureMessage = Assert-FailsWith -ExpectedMessage "missing summary validator failure" -Action {
        Invoke-ResultValidator -JsonPath $missingFailurePath -MarkdownPath $resultMarkdownFullPath
    }

    $badMarkdownPath = Join-Path $tempRoot "bad-result.md"
    $badMarkdown = $markdown.Replace("- CI summary validator regression: ``passed``", "- CI summary validator regression: ``failed``")
    Write-Utf8NoBomFile -PathValue $badMarkdownPath -Content $badMarkdown
    $badMarkdownMessage = Assert-FailsWith -ExpectedMessage "markdown is missing" -Action {
        Invoke-ResultValidator -JsonPath $resultJsonFullPath -MarkdownPath $badMarkdownPath
    }

    $regression = [ordered]@{
        status = "passed"
        resultJsonPath = $resultJsonFullPath
        resultMarkdownPath = $resultMarkdownFullPath
        releaseId = [string]$valid.releaseId
        summaryValidatorTestedFailuresCount = [int]$valid.summaryValidatorTestedFailuresCount
        testedFailures = @(
            [ordered]@{ name = "bad-status"; message = $badStatusMessage },
            [ordered]@{ name = "missing-release-id"; message = $missingReleaseMessage },
            [ordered]@{ name = "missing-summary-validator-failure"; message = $missingFailureMessage },
            [ordered]@{ name = "bad-markdown"; message = $badMarkdownMessage }
        )
    }

    if ($WriteJson) {
        Write-Output ($regression | ConvertTo-Json -Depth 8)
    }
    else {
        Write-Host "production evidence handoff package archive CI regression result validator regression passed $($regression | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
