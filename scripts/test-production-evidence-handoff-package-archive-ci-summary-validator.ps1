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

    return & (Resolve-RepoPath "scripts/validate-production-evidence-handoff-package-archive-ci-summary.ps1") `
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
    throw "Production evidence handoff package archive CI summary validator result JSON was not found: $ResultJsonPath"
}

if ([string]::IsNullOrWhiteSpace($SummaryPath) -or -not (Test-Path -LiteralPath $SummaryPath -PathType Leaf)) {
    throw "Production evidence handoff package archive CI summary validator Markdown was not found: $SummaryPath"
}

$resultJsonFullPath = (Resolve-Path -LiteralPath $ResultJsonPath).Path
$summaryFullPath = (Resolve-Path -LiteralPath $SummaryPath).Path
$result = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
$summary = Get-Content -LiteralPath $summaryFullPath -Raw -Encoding UTF8

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("vpn-platform-ci-summary-validator-test-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

try {
    $validJson = Invoke-SummaryValidator -JsonPath $resultJsonFullPath -MarkdownPath $summaryFullPath
    $valid = $validJson | ConvertFrom-Json

    $badStatus = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badStatus.mainFlow.status = "failed"
    $badStatusPath = Copy-ResultJson -Source $badStatus -DestinationPath (Join-Path $tempRoot "bad-main-flow-status.json")
    $badStatusMessage = Assert-FailsWith -ExpectedMessage "main flow status must be passed" -Action {
        Invoke-SummaryValidator -JsonPath $badStatusPath -MarkdownPath $summaryFullPath
    }

    $badReleasePath = Join-Path $tempRoot "bad-release-summary.md"
    $badRelease = $summary.Replace([string]$result.releaseId, "wrong-release-id")
    Write-Utf8NoBomFile -PathValue $badReleasePath -Content $badRelease
    $badReleaseMessage = Assert-FailsWith -ExpectedMessage "markdown is missing" -Action {
        Invoke-SummaryValidator -JsonPath $resultJsonFullPath -MarkdownPath $badReleasePath
    }

    $missingArtifactPath = Join-Path $tempRoot "missing-artifact-summary.md"
    $missingArtifact = $summary.Replace([string]$result.resultJsonPath, "missing-ci-regression-result.json")
    Write-Utf8NoBomFile -PathValue $missingArtifactPath -Content $missingArtifact
    $missingArtifactMessage = Assert-FailsWith -ExpectedMessage "markdown is missing" -Action {
        Invoke-SummaryValidator -JsonPath $resultJsonFullPath -MarkdownPath $missingArtifactPath
    }

    $missingLongPathStatusPath = Join-Path $tempRoot "missing-long-path-status-summary.md"
    $missingLongPathStatus = $summary.Replace("- Long path regression: ``passed``", "- Long path regression: ``failed``")
    Write-Utf8NoBomFile -PathValue $missingLongPathStatusPath -Content $missingLongPathStatus
    $missingLongPathStatusMessage = Assert-FailsWith -ExpectedMessage "markdown is missing" -Action {
        Invoke-SummaryValidator -JsonPath $resultJsonFullPath -MarkdownPath $missingLongPathStatusPath
    }

    $regression = [ordered]@{
        status = "passed"
        resultJsonPath = $resultJsonFullPath
        summaryPath = $summaryFullPath
        releaseId = [string]$valid.releaseId
        testedFailures = @(
            [ordered]@{ name = "bad-main-flow-status"; message = $badStatusMessage },
            [ordered]@{ name = "bad-release-summary"; message = $badReleaseMessage },
            [ordered]@{ name = "missing-artifact-path"; message = $missingArtifactMessage },
            [ordered]@{ name = "bad-long-path-status"; message = $missingLongPathStatusMessage }
        )
    }

    if ($WriteJson) {
        Write-Output ($regression | ConvertTo-Json -Depth 8)
    }
    else {
        Write-Host "production evidence handoff package archive CI summary validator regression passed $($regression | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
