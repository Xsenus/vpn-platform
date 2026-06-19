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

    return & (Resolve-RepoPath "scripts/validate-production-readiness-assertion-ci-regression-result.ps1") @validatorArgs
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
    throw "Production readiness assertion CI regression result validator JSON was not found: $ResultJsonPath"
}

$resultJsonFullPath = (Resolve-Path -LiteralPath $ResultJsonPath).Path
$result = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ([string]::IsNullOrWhiteSpace($ResultMarkdownPath)) {
    $ResultMarkdownPath = [string]$result.resultMarkdownPath
}

if ([string]::IsNullOrWhiteSpace($ResultMarkdownPath) -or -not (Test-Path -LiteralPath $ResultMarkdownPath -PathType Leaf)) {
    throw "Production readiness assertion CI regression result validator Markdown was not found: $ResultMarkdownPath"
}

$resultMarkdownFullPath = (Resolve-Path -LiteralPath $ResultMarkdownPath).Path
$markdown = Get-Content -LiteralPath $resultMarkdownFullPath -Raw -Encoding UTF8

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("vpn-platform-readiness-ci-result-validator-test-" + [Guid]::NewGuid().ToString("N"))
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

    $badAssertionExitCode = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badAssertionExitCode.assertion.exitCode = 0
    $badAssertionExitCodePath = Copy-ResultJson -Source $badAssertionExitCode -DestinationPath (Join-Path $tempRoot "bad-assertion-exit-code-result.json")
    $badAssertionExitCodeMessage = Assert-FailsWith -ExpectedMessage "blocked assertion must have exitCode 1" -Action {
        Invoke-ResultValidator -JsonPath $badAssertionExitCodePath -MarkdownPath $resultMarkdownFullPath
    }

    $missingRegressionFailure = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $missingRegressionFailure.resultValidatorRegression.testedFailures = @(
        $missingRegressionFailure.resultValidatorRegression.testedFailures |
            Where-Object { [string]$_.name -ne "bad-markdown" }
    )
    $missingRegressionFailurePath = Copy-ResultJson -Source $missingRegressionFailure -DestinationPath (Join-Path $tempRoot "missing-regression-failure-result.json")
    $missingRegressionFailureMessage = Assert-FailsWith -ExpectedMessage "missing validator regression failure: bad-markdown" -Action {
        Invoke-ResultValidator -JsonPath $missingRegressionFailurePath -MarkdownPath $resultMarkdownFullPath
    }

    $badMarkdownPath = Join-Path $tempRoot "bad-result.md"
    $badMarkdown = $markdown.Replace("- Result validator regression: ``passed``", "- Result validator regression: ``failed``")
    Write-Utf8NoBomFile -PathValue $badMarkdownPath -Content $badMarkdown
    $badMarkdownResult = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badMarkdownJsonPath = Join-Path $tempRoot "bad-markdown-result.json"
    $badMarkdownResult.resultJsonPath = $badMarkdownJsonPath
    $badMarkdownResult.resultMarkdownPath = $badMarkdownPath
    Copy-ResultJson -Source $badMarkdownResult -DestinationPath $badMarkdownJsonPath | Out-Null
    $badMarkdownMessage = Assert-FailsWith -ExpectedMessage "markdown is missing" -Action {
        Invoke-ResultValidator -JsonPath $badMarkdownJsonPath -MarkdownPath $badMarkdownPath
    }

    $wrongValidatorCount = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $wrongValidatorCount.resultValidator.evidenceReportsCount = 3
    $wrongValidatorCountPath = Copy-ResultJson -Source $wrongValidatorCount -DestinationPath (Join-Path $tempRoot "wrong-validator-count-result.json")
    $wrongValidatorCountMessage = Assert-FailsWith -ExpectedMessage "evidenceReportsCount must be 4" -Action {
        Invoke-ResultValidator -JsonPath $wrongValidatorCountPath -MarkdownPath $resultMarkdownFullPath
    }

    $regression = [ordered]@{
        status = "passed"
        resultJsonPath = $resultJsonFullPath
        resultMarkdownPath = $resultMarkdownFullPath
        assertionStatus = [string]$valid.assertionStatus
        testedFailures = @(
            [ordered]@{ name = "bad-status"; message = $badStatusMessage },
            [ordered]@{ name = "bad-assertion-exit-code"; message = $badAssertionExitCodeMessage },
            [ordered]@{ name = "missing-regression-failure"; message = $missingRegressionFailureMessage },
            [ordered]@{ name = "bad-markdown"; message = $badMarkdownMessage },
            [ordered]@{ name = "wrong-validator-count"; message = $wrongValidatorCountMessage }
        )
    }

    if ($WriteJson) {
        Write-Output ($regression | ConvertTo-Json -Depth 8)
    }
    else {
        Write-Host "production readiness assertion CI regression result validator regression passed $($regression | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
