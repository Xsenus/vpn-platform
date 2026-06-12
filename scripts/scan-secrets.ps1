param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path $Root).Path.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)

$excludeDirectories = @(
    '.git',
    '.serena',
    '.playwright-mcp',
    'node_modules',
    'bin',
    'obj',
    'dist',
    'build',
    'TestResults',
    'artifacts',
    'coverage',
    'playwright-report',
    'backups'
)

$includeExtensions = @(
    '.cs',
    '.csproj',
    '.json',
    '.md',
    '.ps1',
    '.sh',
    '.ts',
    '.tsx',
    '.js',
    '.jsx',
    '.css',
    '.html',
    '.yml',
    '.yaml',
    '.env',
    '.example',
    '.config',
    '.conf',
    '.log',
    '.txt',
    '.sql'
)

$includeNames = @(
    '.env.example',
    '.gitignore',
    'Dockerfile',
    'docker-compose.yml',
    'docker-compose.validation.yml'
)

$secretPatterns = @(
    @{ Name = 'Telegram bot token'; Pattern = '\b\d{8,10}:AA[A-Za-z0-9_-]{30,}\b' },
    @{ Name = 'Stripe/OpenAI style API key'; Pattern = '\b(?:sk|rk|pk)_(?:live|test)_[A-Za-z0-9]{16,}\b|\bsk-(?:proj-|svcacct-)?[A-Za-z0-9_-]{32,}\b' },
    @{ Name = 'GitHub token'; Pattern = '\bgh[pousr]_[A-Za-z0-9_]{30,}\b' },
    @{ Name = 'GitLab token'; Pattern = '\bglpat-[A-Za-z0-9_-]{20,}\b' },
    @{ Name = 'AWS access key'; Pattern = '\bAKIA[0-9A-Z]{16}\b' },
    @{ Name = 'Google API key'; Pattern = '\bAIza[0-9A-Za-z_-]{35}\b' },
    @{ Name = 'Slack token'; Pattern = '\bxox[baprs]-[A-Za-z0-9-]{20,}\b' },
    @{ Name = 'Private key PEM'; Pattern = '-----BEGIN (?:RSA |OPENSSH |EC |DSA )?PRIVATE KEY-----' }
)

function Test-SkippedPath {
    param([string]$Path)
    $relative = Get-RelativePath $Path
    foreach ($part in $relative.Split('/')) {
        if ($excludeDirectories -contains $part) {
            return $true
        }
    }

    return $false
}

function Get-RelativePath {
    param([string]$Path)
    $fullPath = (Resolve-Path $Path).Path
    if ($fullPath.StartsWith($Root, [StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($Root.Length).TrimStart('\', '/').Replace('\', '/')
    }

    return $fullPath.Replace('\', '/')
}

function Test-TextFile {
    param([IO.FileInfo]$File)
    if ($includeNames -contains $File.Name) {
        return $true
    }

    if ($File.Name -like 'Dockerfile*') {
        return $true
    }

    return $includeExtensions -contains $File.Extension
}

function Test-AllowedFixture {
    param([string]$RelativePath, [string]$Line)
    if ($RelativePath -match '^(backend|frontend)/tests/') {
        return $true
    }

    if ($RelativePath -match '^scripts/scan-secrets\.(ps1|sh)$') {
        return $true
    }

    return $Line -match '(?i)(placeholder|example|change-me|local-dev|local-validation|schema-audit|ef-drift|dummy|fixture|must-not-leak|redacted)'
}

$findings = New-Object System.Collections.Generic.List[string]
$files = Get-ChildItem -LiteralPath $Root -Recurse -File -Force |
    Where-Object { -not (Test-SkippedPath $_.FullName) -and (Test-TextFile $_) }

foreach ($file in $files) {
    $relative = Get-RelativePath $file.FullName
    try {
        $lines = [IO.File]::ReadAllLines($file.FullName)
    }
    catch {
        continue
    }

    for ($i = 0; $i -lt $lines.Length; $i++) {
        foreach ($secretPattern in $secretPatterns) {
            if ($lines[$i] -match $secretPattern.Pattern -and -not (Test-AllowedFixture $relative $lines[$i])) {
                $findings.Add("${relative}:$($i + 1): $($secretPattern.Name)")
            }
        }
    }
}

if ($findings.Count -gt 0) {
    Write-Error ("Secret scan failed:`n" + ($findings -join "`n"))
    exit 1
}

Write-Host "[OK] secret scan completed. Files scanned: $($files.Count). Findings: 0."
