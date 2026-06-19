param(
    [Parameter(Mandatory = $true)]
    [string]$ResultJsonPath,

    [string]$ResultMarkdownPath = "",
    [switch]$RequireBlockedAssertion,
    [switch]$WriteJson
)

$ErrorActionPreference = "Stop"

function Assert-ExistingFile {
    param(
        [string]$PathValue,
        [string]$Label
    )

    if ([string]::IsNullOrWhiteSpace($PathValue) -or -not (Test-Path -LiteralPath $PathValue -PathType Leaf)) {
        throw "Production readiness assertion CI regression result $Label was not found: $PathValue"
    }

    return (Resolve-Path -LiteralPath $PathValue).Path
}

function Assert-ExistingDirectory {
    param(
        [string]$PathValue,
        [string]$Label
    )

    if ([string]::IsNullOrWhiteSpace($PathValue) -or -not (Test-Path -LiteralPath $PathValue -PathType Container)) {
        throw "Production readiness assertion CI regression result $Label was not found: $PathValue"
    }

    return (Resolve-Path -LiteralPath $PathValue).Path
}

function Assert-MarkdownContains {
    param(
        [string]$Markdown,
        [string]$Expected
    )

    if ($Markdown.IndexOf($Expected, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Production readiness assertion CI regression result markdown is missing: $Expected"
    }
}

function Assert-RegressionFailure {
    param(
        [object[]]$TestedFailures,
        [string]$Name
    )

    $failure = $TestedFailures | Where-Object { [string]$_.name -eq $Name } | Select-Object -First 1
    if ($null -eq $failure) {
        throw "Production readiness assertion CI regression result is missing validator regression failure: $Name"
    }

    if ([string]::IsNullOrWhiteSpace([string]$failure.message)) {
        throw "Production readiness assertion CI regression result validator regression failure has empty message: $Name"
    }
}

$resultJsonFullPath = Assert-ExistingFile -PathValue $ResultJsonPath -Label "JSON"
$result = Get-Content -LiteralPath $resultJsonFullPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ([string]$result.status -ne "passed") {
    throw "Production readiness assertion CI regression result status must be passed."
}

$outputDirectory = Assert-ExistingDirectory -PathValue ([string]$result.outputDirectory) -Label "output directory"
$assertion = $result.assertion
if ($null -eq $assertion) {
    throw "Production readiness assertion CI regression result assertion section is required."
}

$assertionStatus = [string]$assertion.status
if ($assertionStatus -notin @("blocked", "production-ready")) {
    throw "Production readiness assertion CI regression result assertion status must be blocked or production-ready."
}

if ($RequireBlockedAssertion -and $assertionStatus -ne "blocked") {
    throw "Production readiness assertion CI regression result must contain blocked assertion when -RequireBlockedAssertion is set."
}

$assertionExitCode = [int]$assertion.exitCode
if ($assertionStatus -eq "blocked" -and $assertionExitCode -ne 1) {
    throw "Production readiness assertion CI regression result blocked assertion must have exitCode 1."
}

if ($assertionStatus -eq "production-ready" -and $assertionExitCode -ne 0) {
    throw "Production readiness assertion CI regression result production-ready assertion must have exitCode 0."
}

$assertionJsonPath = Assert-ExistingFile -PathValue ([string]$assertion.resultJsonPath) -Label "assertion JSON"
$assertionMarkdownPath = Assert-ExistingFile -PathValue ([string]$assertion.resultMarkdownPath) -Label "assertion Markdown"
$assertionLogPath = Assert-ExistingFile -PathValue ([string]$assertion.logPath) -Label "assertion log"

foreach ($countName in @("failedEvidenceReportsCount", "blockersCount")) {
    if ([int]$assertion.PSObject.Properties[$countName].Value -lt 0) {
        throw "Production readiness assertion CI regression result assertion $countName must be non-negative."
    }
}

$resultValidator = $result.resultValidator
if ($null -eq $resultValidator -or [string]$resultValidator.status -ne "valid") {
    throw "Production readiness assertion CI regression result validator status must be valid."
}

if ([string]$resultValidator.assertionStatus -ne $assertionStatus) {
    throw "Production readiness assertion CI regression result validator assertionStatus does not match assertion status."
}

if ([int]$resultValidator.failedEvidenceReportsCount -ne [int]$assertion.failedEvidenceReportsCount) {
    throw "Production readiness assertion CI regression result validator failedEvidenceReportsCount does not match assertion."
}

if ([int]$resultValidator.blockersCount -ne [int]$assertion.blockersCount) {
    throw "Production readiness assertion CI regression result validator blockersCount does not match assertion."
}

if ([int]$resultValidator.evidenceReportsCount -ne 4) {
    throw "Production readiness assertion CI regression result validator evidenceReportsCount must be 4."
}

$regression = $result.resultValidatorRegression
if ($null -eq $regression) {
    throw "Production readiness assertion CI regression result validator regression section is required."
}

if ($assertionStatus -eq "blocked") {
    if ([string]$regression.status -ne "passed") {
        throw "Production readiness assertion CI regression result validator regression status must be passed for blocked assertion."
    }

    $testedFailures = @($regression.testedFailures)
    foreach ($expectedFailure in @(
            "bad-status",
            "bad-failed-evidence-count",
            "missing-evidence-report",
            "bad-markdown",
            "require-production-ready"
        )) {
        Assert-RegressionFailure -TestedFailures $testedFailures -Name $expectedFailure
    }
}
else {
    if ([string]$regression.status -ne "skipped") {
        throw "Production readiness assertion CI regression result validator regression status must be skipped for production-ready assertion."
    }
}

if ([string]::IsNullOrWhiteSpace($ResultMarkdownPath)) {
    $ResultMarkdownPath = [string]$result.resultMarkdownPath
}

$resultMarkdownFullPath = Assert-ExistingFile -PathValue $ResultMarkdownPath -Label "Markdown"
if ($resultMarkdownFullPath -ne (Assert-ExistingFile -PathValue ([string]$result.resultMarkdownPath) -Label "linked Markdown")) {
    throw "Production readiness assertion CI regression result Markdown path does not match resultMarkdownPath."
}

$linkedResultJsonPath = Assert-ExistingFile -PathValue ([string]$result.resultJsonPath) -Label "linked JSON"
if ($linkedResultJsonPath -ne $resultJsonFullPath) {
    throw "Production readiness assertion CI regression result JSON path does not match resultJsonPath."
}

$markdown = Get-Content -LiteralPath $resultMarkdownFullPath -Raw -Encoding UTF8
foreach ($expected in @(
        "# Production readiness assertion CI regression",
        "- Status: ``passed``",
        "- Assertion status: ``$assertionStatus``",
        "- Failed evidence reports: ``$([int]$assertion.failedEvidenceReportsCount)``",
        "- Blockers: ``$([int]$assertion.blockersCount)``",
        "- Result validator: ``valid``",
        "- Result validator regression: ``$([string]$regression.status)``",
        "## Artifacts",
        "production-readiness-assertion.json",
        "production-readiness-assertion.md",
        "production-readiness-assertion.log",
        "production-readiness-assertion-ci-regression-result.json",
        "production-readiness-assertion-ci-regression-result.md"
    )) {
    Assert-MarkdownContains -Markdown $markdown -Expected $expected
}

$validation = [ordered]@{
    status = "valid"
    resultJsonPath = $resultJsonFullPath
    resultMarkdownPath = $resultMarkdownFullPath
    outputDirectory = $outputDirectory
    assertionStatus = $assertionStatus
    assertionExitCode = $assertionExitCode
    failedEvidenceReportsCount = [int]$assertion.failedEvidenceReportsCount
    blockersCount = [int]$assertion.blockersCount
    resultValidatorStatus = [string]$resultValidator.status
    resultValidatorRegressionStatus = [string]$regression.status
    assertionJsonPath = $assertionJsonPath
    assertionMarkdownPath = $assertionMarkdownPath
    assertionLogPath = $assertionLogPath
}

if ($WriteJson) {
    Write-Output ($validation | ConvertTo-Json -Depth 6)
}
else {
    Write-Host "production readiness assertion CI regression result valid $($validation | ConvertTo-Json -Depth 6 -Compress)"
}
