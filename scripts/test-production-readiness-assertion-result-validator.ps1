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
        [string]$MarkdownPath = "",
        [switch]$RequireProductionReady
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

    return & (Resolve-RepoPath "scripts/validate-production-readiness-assertion-result.ps1") @validatorArgs
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

    if ($Source.PSObject.Properties.Name -contains "resultJsonPath") {
        $Source.resultJsonPath = $DestinationPath
    }

    Write-Utf8NoBomFile -PathValue $DestinationPath -Content ($Source | ConvertTo-Json -Depth 12)
    return $DestinationPath
}

if ([string]::IsNullOrWhiteSpace($ResultJsonPath) -or -not (Test-Path -LiteralPath $ResultJsonPath -PathType Leaf)) {
    throw "Production readiness assertion result validator regression JSON was not found: $ResultJsonPath"
}

$resultJsonFullPath = (Resolve-Path -LiteralPath $ResultJsonPath).Path
$result = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ([string]::IsNullOrWhiteSpace($ResultMarkdownPath)) {
    $ResultMarkdownPath = [string]$result.resultMarkdownPath
}

if ([string]::IsNullOrWhiteSpace($ResultMarkdownPath) -or -not (Test-Path -LiteralPath $ResultMarkdownPath -PathType Leaf)) {
    throw "Production readiness assertion result validator regression Markdown was not found: $ResultMarkdownPath"
}

$resultMarkdownFullPath = (Resolve-Path -LiteralPath $ResultMarkdownPath).Path
$markdown = Get-Content -LiteralPath $resultMarkdownFullPath -Raw -Encoding UTF8

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("vpn-platform-readiness-assertion-result-validator-test-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

try {
    $validJson = Invoke-ResultValidator -JsonPath $resultJsonFullPath -MarkdownPath $resultMarkdownFullPath
    $valid = $validJson | ConvertFrom-Json

    $badStatus = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badStatus.status = "failed"
    $badStatusPath = Copy-ResultJson -Source $badStatus -DestinationPath (Join-Path $tempRoot "bad-status-result.json")
    $badStatusMessage = Assert-FailsWith -ExpectedMessage "status must be blocked or production-ready" -Action {
        Invoke-ResultValidator -JsonPath $badStatusPath -MarkdownPath $resultMarkdownFullPath
    }

    $badFailedCount = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badFailedCount.failedEvidenceReportsCount = [int]$badFailedCount.failedEvidenceReportsCount + 1
    $badFailedCountPath = Copy-ResultJson -Source $badFailedCount -DestinationPath (Join-Path $tempRoot "bad-failed-count-result.json")
    $badFailedCountMessage = Assert-FailsWith -ExpectedMessage "failedEvidenceReportsCount does not match" -Action {
        Invoke-ResultValidator -JsonPath $badFailedCountPath -MarkdownPath $resultMarkdownFullPath
    }

    $missingReport = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $missingReport.evidenceReports = @(
        $missingReport.evidenceReports |
            Where-Object { [string]$_.name -ne "vpn-live" }
    )
    $missingReportPath = Copy-ResultJson -Source $missingReport -DestinationPath (Join-Path $tempRoot "missing-evidence-report-result.json")
    $missingReportMessage = Assert-FailsWith -ExpectedMessage "missing evidence report: vpn-live" -Action {
        Invoke-ResultValidator -JsonPath $missingReportPath -MarkdownPath $resultMarkdownFullPath
    }

    $badMarkdownPath = Join-Path $tempRoot "bad-result.md"
    $badMarkdown = $markdown.Replace("## Evidence reports", "## Evidence reportz")
    Write-Utf8NoBomFile -PathValue $badMarkdownPath -Content $badMarkdown
    $badMarkdownResult = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $badMarkdownResult.resultMarkdownPath = $badMarkdownPath
    $badMarkdownResultPath = Copy-ResultJson -Source $badMarkdownResult -DestinationPath (Join-Path $tempRoot "bad-markdown-result.json")
    $badMarkdownMessage = Assert-FailsWith -ExpectedMessage "markdown is missing" -Action {
        Invoke-ResultValidator -JsonPath $badMarkdownResultPath -MarkdownPath $badMarkdownPath
    }

    $requireProductionReadyMessage = Assert-FailsWith -ExpectedMessage "must be production-ready" -Action {
        Invoke-ResultValidator -JsonPath $resultJsonFullPath -MarkdownPath $resultMarkdownFullPath -RequireProductionReady
    }

    $regression = [ordered]@{
        status = "passed"
        assertionStatus = [string]$valid.assertionStatus
        resultJsonPath = $resultJsonFullPath
        resultMarkdownPath = $resultMarkdownFullPath
        failedEvidenceReportsCount = [int]$valid.failedEvidenceReportsCount
        blockersCount = [int]$valid.blockersCount
        evidenceReportsCount = [int]$valid.evidenceReportsCount
        testedFailures = @(
            [ordered]@{ name = "bad-status"; message = $badStatusMessage },
            [ordered]@{ name = "bad-failed-evidence-count"; message = $badFailedCountMessage },
            [ordered]@{ name = "missing-evidence-report"; message = $missingReportMessage },
            [ordered]@{ name = "bad-markdown"; message = $badMarkdownMessage },
            [ordered]@{ name = "require-production-ready"; message = $requireProductionReadyMessage }
        )
    }

    if ($WriteJson) {
        Write-Output ($regression | ConvertTo-Json -Depth 8)
    }
    else {
        Write-Host "production readiness assertion result validator regression passed $($regression | ConvertTo-Json -Depth 8 -Compress)"
    }
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
