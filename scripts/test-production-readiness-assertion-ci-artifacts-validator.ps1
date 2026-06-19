param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactDirectory,

    [string]$StepSummaryPath = "",
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

function Invoke-ArtifactsValidator {
    param(
        [string]$DirectoryPath,
        [string]$SummaryPath = ""
    )

    $validatorArgs = @{
        ArtifactDirectory = $DirectoryPath
        RequireBlockedAssertion = $true
        WriteJson = $true
    }

    if (-not [string]::IsNullOrWhiteSpace($SummaryPath)) {
        $validatorArgs.StepSummaryPath = $SummaryPath
    }

    return & (Resolve-RepoPath "scripts/validate-production-readiness-assertion-ci-artifacts.ps1") @validatorArgs
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

function Copy-ArtifactDirectory {
    param(
        [string]$SourceDirectory,
        [string]$DestinationDirectory
    )

    New-Item -ItemType Directory -Path $DestinationDirectory -Force | Out-Null
    Get-ChildItem -LiteralPath $SourceDirectory -Force |
        Copy-Item -Destination $DestinationDirectory -Recurse -Force

    $resultPath = Join-Path $DestinationDirectory "production-readiness-assertion-ci-regression-result.json"
    $resultMarkdownPath = Join-Path $DestinationDirectory "production-readiness-assertion-ci-regression-result.md"
    $result = Get-Content -LiteralPath $resultPath -Raw -Encoding UTF8 | ConvertFrom-Json

    $pathMap = [ordered]@{
        ([string]$result.outputDirectory) = $DestinationDirectory
        ([string]$result.resultJsonPath) = $resultPath
        ([string]$result.resultMarkdownPath) = $resultMarkdownPath
        ([string]$result.assertion.resultJsonPath) = (Join-Path $DestinationDirectory "production-readiness-assertion.json")
        ([string]$result.assertion.resultMarkdownPath) = (Join-Path $DestinationDirectory "production-readiness-assertion.md")
        ([string]$result.assertion.logPath) = (Join-Path $DestinationDirectory "production-readiness-assertion.log")
    }

    $result.outputDirectory = $DestinationDirectory
    $result.resultJsonPath = $resultPath
    $result.resultMarkdownPath = $resultMarkdownPath
    $result.assertion.resultJsonPath = [string]$pathMap[[string]$result.assertion.resultJsonPath]
    $result.assertion.resultMarkdownPath = [string]$pathMap[[string]$result.assertion.resultMarkdownPath]
    $result.assertion.logPath = [string]$pathMap[[string]$result.assertion.logPath]
    Write-Utf8NoBomFile -PathValue $resultPath -Content ($result | ConvertTo-Json -Depth 14)

    $markdown = Get-Content -LiteralPath $resultMarkdownPath -Raw -Encoding UTF8
    foreach ($entry in $pathMap.GetEnumerator()) {
        if (-not [string]::IsNullOrWhiteSpace([string]$entry.Key)) {
            $markdown = $markdown.Replace([string]$entry.Key, [string]$entry.Value)
        }
    }
    Write-Utf8NoBomFile -PathValue $resultMarkdownPath -Content $markdown

    return $DestinationDirectory
}

if ([string]::IsNullOrWhiteSpace($ArtifactDirectory) -or -not (Test-Path -LiteralPath $ArtifactDirectory -PathType Container)) {
    throw "Production readiness assertion CI artifacts validator regression directory was not found: $ArtifactDirectory"
}

$artifactDirectoryFullPath = (Resolve-Path -LiteralPath $ArtifactDirectory).Path

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("vpn-platform-readiness-ci-artifacts-validator-test-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

try {
    $validJson = Invoke-ArtifactsValidator -DirectoryPath $artifactDirectoryFullPath -SummaryPath $StepSummaryPath
    $valid = $validJson | ConvertFrom-Json

    $missingFileDirectory = Copy-ArtifactDirectory -SourceDirectory $artifactDirectoryFullPath -DestinationDirectory (Join-Path $tempRoot "missing-file")
    Remove-Item -LiteralPath (Join-Path $missingFileDirectory "production-readiness-assertion.log") -Force
    $missingRequiredArtifactMessage = Assert-FailsWith -ExpectedMessage "assertionLog was not found" -Action {
        Invoke-ArtifactsValidator -DirectoryPath $missingFileDirectory
    }

    $badOutputDirectory = Copy-ArtifactDirectory -SourceDirectory $artifactDirectoryFullPath -DestinationDirectory (Join-Path $tempRoot "bad-output-directory")
    $badOutputResultPath = Join-Path $badOutputDirectory "production-readiness-assertion-ci-regression-result.json"
    $badOutputResult = Get-Content -LiteralPath $badOutputResultPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badOutputResult.outputDirectory = $tempRoot
    Write-Utf8NoBomFile -PathValue $badOutputResultPath -Content ($badOutputResult | ConvertTo-Json -Depth 14)
    $badOutputDirectoryMessage = Assert-FailsWith -ExpectedMessage "outputDirectory does not match artifact directory" -Action {
        Invoke-ArtifactsValidator -DirectoryPath $badOutputDirectory
    }

    $badAssertionLogPathDirectory = Copy-ArtifactDirectory -SourceDirectory $artifactDirectoryFullPath -DestinationDirectory (Join-Path $tempRoot "bad-assertion-log-path")
    $badAssertionLogResultPath = Join-Path $badAssertionLogPathDirectory "production-readiness-assertion-ci-regression-result.json"
    $badAssertionLogResult = Get-Content -LiteralPath $badAssertionLogResultPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $expectedAssertionLogPath = [string]$badAssertionLogResult.assertion.logPath
    $badAssertionLogResult.assertion.logPath = Join-Path $badAssertionLogPathDirectory "unexpected-production-readiness-assertion.log"
    New-Item -ItemType File -Path ([string]$badAssertionLogResult.assertion.logPath) -Force | Out-Null
    Write-Utf8NoBomFile -PathValue $badAssertionLogResultPath -Content ($badAssertionLogResult | ConvertTo-Json -Depth 14)
    $badAssertionLogMarkdownPath = Join-Path $badAssertionLogPathDirectory "production-readiness-assertion-ci-regression-result.md"
    $badAssertionLogMarkdown = Get-Content -LiteralPath $badAssertionLogMarkdownPath -Raw -Encoding UTF8
    $badAssertionLogMarkdown = $badAssertionLogMarkdown.Replace($expectedAssertionLogPath, [string]$badAssertionLogResult.assertion.logPath)
    Write-Utf8NoBomFile -PathValue $badAssertionLogMarkdownPath -Content $badAssertionLogMarkdown
    $badAssertionLogPathMessage = Assert-FailsWith -ExpectedMessage "assertion log path does not match expected artifact file" -Action {
        Invoke-ArtifactsValidator -DirectoryPath $badAssertionLogPathDirectory
    }

    $badResultMarkdownDirectory = Copy-ArtifactDirectory -SourceDirectory $artifactDirectoryFullPath -DestinationDirectory (Join-Path $tempRoot "bad-result-markdown")
    $badResultMarkdownPath = Join-Path $badResultMarkdownDirectory "production-readiness-assertion-ci-regression-result.md"
    $badResultMarkdown = Get-Content -LiteralPath $badResultMarkdownPath -Raw -Encoding UTF8
    $badResultMarkdown = $badResultMarkdown.Replace("- Result validator: ``valid``", "- Result validator: ``invalid``")
    Write-Utf8NoBomFile -PathValue $badResultMarkdownPath -Content $badResultMarkdown
    $badResultMarkdownMessage = Assert-FailsWith -ExpectedMessage "markdown is missing" -Action {
        Invoke-ArtifactsValidator -DirectoryPath $badResultMarkdownDirectory
    }

    $badStepSummaryDirectory = Copy-ArtifactDirectory -SourceDirectory $artifactDirectoryFullPath -DestinationDirectory (Join-Path $tempRoot "bad-step-summary")
    $badStepSummaryPath = Join-Path $badStepSummaryDirectory "production-readiness-assertion-ci-step-summary.md"
    Copy-Item -LiteralPath (Join-Path $badStepSummaryDirectory "production-readiness-assertion-ci-regression-result.md") -Destination $badStepSummaryPath -Force
    $badStepSummary = Get-Content -LiteralPath $badStepSummaryPath -Raw -Encoding UTF8
    $badStepSummary = $badStepSummary.Replace("- Assertion status: ``blocked``", "- Assertion status: ``production-ready``")
    Write-Utf8NoBomFile -PathValue $badStepSummaryPath -Content $badStepSummary
    $badStepSummaryMessage = Assert-FailsWith -ExpectedMessage "markdown is missing" -Action {
        Invoke-ArtifactsValidator -DirectoryPath $badStepSummaryDirectory -SummaryPath $badStepSummaryPath
    }

    $regression = [ordered]@{
        status = "passed"
        artifactDirectory = $artifactDirectoryFullPath
        assertionStatus = [string]$valid.assertionStatus
        requiredArtifactsCount = [int]$valid.requiredArtifactsCount
        testedFailures = @(
            [ordered]@{ name = "missing-required-artifact"; message = $missingRequiredArtifactMessage },
            [ordered]@{ name = "bad-output-directory"; message = $badOutputDirectoryMessage },
            [ordered]@{ name = "bad-assertion-log-path"; message = $badAssertionLogPathMessage },
            [ordered]@{ name = "bad-result-markdown"; message = $badResultMarkdownMessage },
            [ordered]@{ name = "bad-step-summary"; message = $badStepSummaryMessage }
        )
    }

    if ($WriteJson) {
        Write-Output ($regression | ConvertTo-Json -Depth 8)
    }
    else {
        Write-Host "production readiness assertion CI artifacts validator regression passed $($regression | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
