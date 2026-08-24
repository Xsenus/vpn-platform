param(
    [Parameter(Mandatory = $true)][string]$Path,
    [string]$OutputPath = $Path,
    [switch]$RequireStartupReady,
    [switch]$AllowDisabledEmail
)

$ErrorActionPreference = "Stop"

function Get-ProductionEnvValues {
    param([Parameter(Mandatory = $true)][string]$EnvPath)

    $values = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $lineNumber = 0
    foreach ($line in [System.IO.File]::ReadAllLines($EnvPath)) {
        $lineNumber++
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith("#")) {
            continue
        }

        $separatorIndex = $line.IndexOf("=")
        if ($separatorIndex -le 0) {
            throw "Production env startup preflight failed: malformed setting at line $lineNumber."
        }

        $key = $line.Substring(0, $separatorIndex).Trim()
        $value = $line.Substring($separatorIndex + 1).Trim()
        if ($value.Length -ge 2 -and (($value.StartsWith('"') -and $value.EndsWith('"')) -or ($value.StartsWith("'") -and $value.EndsWith("'")))) {
            $value = $value.Substring(1, $value.Length - 2)
        }

        if ($values.ContainsKey($key)) {
            throw "Production env startup preflight failed: duplicate setting $key."
        }

        $values.Add($key, $value)
    }

    return $values
}

function Assert-ProductionStartupReady {
    param([Parameter(Mandatory = $true)][string]$EnvPath)

    $values = Get-ProductionEnvValues -EnvPath $EnvPath
    $errors = [System.Collections.Generic.List[string]]::new()

    $emailMode = if ($values.ContainsKey("Email__Mode")) { $values["Email__Mode"] } else { "" }
    $allowDisabledText = if ($values.ContainsKey("Email__AllowDisabledInProduction")) { $values["Email__AllowDisabledInProduction"] } else { "" }
    $degradedEmailMode = [string]::Equals($emailMode, "Disabled", [System.StringComparison]::OrdinalIgnoreCase) -and
        [string]::Equals($allowDisabledText, "true", [System.StringComparison]::OrdinalIgnoreCase)
    $smtpMode = [string]::Equals($emailMode, "Smtp", [System.StringComparison]::OrdinalIgnoreCase)
    if (-not $smtpMode -and -not $degradedEmailMode) {
        $errors.Add("Email__Mode must be Smtp")
    }

    if (-not $degradedEmailMode) {
        $emailHost = if ($values.ContainsKey("Email__Host")) { $values["Email__Host"] } else { "" }
        if ([string]::IsNullOrWhiteSpace($emailHost)) {
            $errors.Add("Email__Host is required")
        }

        $emailPortText = if ($values.ContainsKey("Email__Port")) { $values["Email__Port"] } else { "" }
        $emailPort = 0
        if (-not [int]::TryParse($emailPortText, [ref]$emailPort) -or $emailPort -lt 1 -or $emailPort -gt 65535) {
            $errors.Add("Email__Port must be between 1 and 65535")
        }

        $fromAddress = if ($values.ContainsKey("Email__FromAddress")) { $values["Email__FromAddress"] } else { "" }
        $validFromAddress = $false
        if (-not [string]::IsNullOrWhiteSpace($fromAddress)) {
            try {
                $parsedAddress = [System.Net.Mail.MailAddress]::new($fromAddress)
                $validFromAddress = -not [string]::IsNullOrWhiteSpace($parsedAddress.Address)
            }
            catch {
                $validFromAddress = $false
            }
        }

        if (-not $validFromAddress) {
            $errors.Add("Email__FromAddress must be a valid email address")
        }

        $emailUsername = if ($values.ContainsKey("Email__Username")) { $values["Email__Username"] } else { "" }
        $emailPassword = if ($values.ContainsKey("Email__Password")) { $values["Email__Password"] } else { "" }
        if (-not [string]::IsNullOrWhiteSpace($emailUsername) -and [string]::IsNullOrWhiteSpace($emailPassword)) {
            $errors.Add("Email__Password is required when Email__Username is configured")
        }
    }

    if ($errors.Count -gt 0) {
        throw "Production env startup preflight failed: $($errors -join '; ')."
    }

    if ($degradedEmailMode) {
        Write-Warning "Production env startup preflight passed in degraded email mode. Password reset and email delivery will be disabled."
    } else {
        Write-Host "Production env startup preflight passed. Required SMTP settings are present."
    }
}

if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
    throw "Production env file was not found: $Path"
}

$requiredProductionValues = [ordered]@{
    "ASPNETCORE_ENVIRONMENT" = "Production"
    "AdminBootstrap__Enabled" = "false"
    "AdminBootstrap__Password" = ""
    "AdminBootstrap__ResetExistingPassword" = "false"
    "Database__ApplyMigrationsOnStartup" = "false"
    "Database__SeedDemoData" = "false"
    "Swagger__Enabled" = "false"
    "Email__AllowDisabledInProduction" = "false"
    "Auth__PasswordReset__Enabled" = "true"
}

if ($AllowDisabledEmail) {
    $requiredProductionValues["Email__Mode"] = "Disabled"
    $requiredProductionValues["Email__AllowDisabledInProduction"] = "true"
    $requiredProductionValues["Auth__PasswordReset__Enabled"] = "false"
}

$lines = [System.Collections.Generic.List[string]]::new()
$lines.AddRange([System.IO.File]::ReadAllLines((Resolve-Path -LiteralPath $Path)))
$seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$normalized = [System.Collections.Generic.List[string]]::new()

foreach ($line in $lines) {
    if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith("#") -or -not $line.Contains("=")) {
        $normalized.Add($line)
        continue
    }

    $key = $line.Substring(0, $line.IndexOf("=")).Trim()
    if ($requiredProductionValues.Contains($key)) {
        $normalized.Add("$key=$($requiredProductionValues[$key])")
        [void]$seen.Add($key)
        continue
    }

    $normalized.Add($line)
}

foreach ($key in $requiredProductionValues.Keys) {
    if (-not $seen.Contains($key)) {
        $normalized.Add("$key=$($requiredProductionValues[$key])")
    }
}

$resolvedOutputPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
} else {
    Join-Path (Get-Location) $OutputPath
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllLines($resolvedOutputPath, $normalized, $utf8NoBom)

Write-Host "Production env normalized for deploy. Forced keys: $($requiredProductionValues.Keys -join ', ')"

if ($RequireStartupReady) {
    Assert-ProductionStartupReady -EnvPath $resolvedOutputPath
}
