param(
    [Parameter(Mandatory = $true)]
    [string]$ResultJsonPath,

    [Parameter(Mandatory = $true)]
    [string]$SummaryPath,

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

function Invoke-SummaryValidator {
    param(
        [string]$JsonPath,
        [string]$MarkdownPath
    )

    return & (Resolve-RepoPath "scripts/validate-production-readiness-assertion-ci-summary.ps1") `
        -ResultJsonPath $JsonPath `
        -SummaryPath $MarkdownPath `
        -WriteJson
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
    throw "Production readiness assertion CI summary validator result JSON was not found: $ResultJsonPath"
}

if ([string]::IsNullOrWhiteSpace($SummaryPath) -or -not (Test-Path -LiteralPath $SummaryPath -PathType Leaf)) {
    throw "Production readiness assertion CI summary validator Markdown was not found: $SummaryPath"
}

$resultJsonFullPath = (Resolve-Path -LiteralPath $ResultJsonPath).Path
$summaryFullPath = (Resolve-Path -LiteralPath $SummaryPath).Path
$result = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
$summary = Get-Content -LiteralPath $summaryFullPath -Raw -Encoding UTF8

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("vpn-platform-readiness-ci-summary-validator-test-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

try {
    $validJson = Invoke-SummaryValidator -JsonPath $resultJsonFullPath -MarkdownPath $summaryFullPath
    $valid = $validJson | ConvertFrom-Json

    $badStatus = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badStatus.status = "failed"
    $badStatusPath = Copy-ResultJson -Source $badStatus -DestinationPath (Join-Path $tempRoot "bad-status.json")
    $badStatusMessage = Assert-FailsWith -ExpectedMessage "status must be passed" -Action {
        Invoke-SummaryValidator -JsonPath $badStatusPath -MarkdownPath $summaryFullPath
    }

    $badAssertionStatusPath = Join-Path $tempRoot "bad-assertion-status-summary.md"
    $badAssertionStatus = $summary.Replace("- Assertion status: ``$([string]$result.assertion.status)``", "- Assertion status: ``production-ready``")
    Write-Utf8NoBomFile -PathValue $badAssertionStatusPath -Content $badAssertionStatus
    $badAssertionStatusMessage = Assert-FailsWith -ExpectedMessage "markdown is missing" -Action {
        Invoke-SummaryValidator -JsonPath $resultJsonFullPath -MarkdownPath $badAssertionStatusPath
    }

    $missingArtifactPath = Join-Path $tempRoot "missing-artifact-summary.md"
    $missingArtifact = $summary.Replace([string]$result.resultJsonPath, "missing-production-readiness-assertion-ci-regression-result.json")
    Write-Utf8NoBomFile -PathValue $missingArtifactPath -Content $missingArtifact
    $missingArtifactMessage = Assert-FailsWith -ExpectedMessage "markdown is missing" -Action {
        Invoke-SummaryValidator -JsonPath $resultJsonFullPath -MarkdownPath $missingArtifactPath
    }

    $badResultValidatorRegressionPath = Join-Path $tempRoot "bad-result-validator-regression-summary.md"
    $badResultValidatorRegression = $summary.Replace("- Result validator regression: ``passed``", "- Result validator regression: ``failed``")
    Write-Utf8NoBomFile -PathValue $badResultValidatorRegressionPath -Content $badResultValidatorRegression
    $badResultValidatorRegressionMessage = Assert-FailsWith -ExpectedMessage "markdown is missing" -Action {
        Invoke-SummaryValidator -JsonPath $resultJsonFullPath -MarkdownPath $badResultValidatorRegressionPath
    }

    $badCiResultValidatorRegressionPath = Join-Path $tempRoot "bad-ci-result-validator-regression-summary.md"
    $badCiResultValidatorRegression = $summary.Replace("- CI result validator regression: ``passed``", "- CI result validator regression: ``failed``")
    Write-Utf8NoBomFile -PathValue $badCiResultValidatorRegressionPath -Content $badCiResultValidatorRegression
    $badCiResultValidatorRegressionMessage = Assert-FailsWith -ExpectedMessage "markdown is missing" -Action {
        Invoke-SummaryValidator -JsonPath $resultJsonFullPath -MarkdownPath $badCiResultValidatorRegressionPath
    }

    $regression = [ordered]@{
        status = "passed"
        resultJsonPath = $resultJsonFullPath
        summaryPath = $summaryFullPath
        assertionStatus = [string]$valid.assertionStatus
        testedFailures = @(
            [ordered]@{ name = "bad-status"; message = $badStatusMessage },
            [ordered]@{ name = "bad-assertion-status"; message = $badAssertionStatusMessage },
            [ordered]@{ name = "missing-artifact-path"; message = $missingArtifactMessage },
            [ordered]@{ name = "bad-result-validator-regression"; message = $badResultValidatorRegressionMessage },
            [ordered]@{ name = "bad-ci-result-validator-regression"; message = $badCiResultValidatorRegressionMessage }
        )
    }

    if ($WriteJson) {
        Write-Output ($regression | ConvertTo-Json -Depth 8)
    }
    else {
        Write-Host "production readiness assertion CI summary validator regression passed $($regression | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
