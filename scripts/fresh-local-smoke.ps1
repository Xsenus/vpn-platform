param(
    [int]$ApiPort = 18101,
    [switch]$KeepArtifacts
)

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$tmp = Join-Path $root "tmp\fresh-local-smoke"
$tmpRoot = Join-Path $root "tmp"
$resolvedRoot = (Resolve-Path $root).Path

function Assert-InWorkspace {
    param([string]$Path)

    $fullPath = if (Test-Path $Path) { (Resolve-Path $Path).Path } else { [System.IO.Path]::GetFullPath($Path) }
    if (-not $fullPath.StartsWith($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to touch path outside workspace: $fullPath"
    }
}

function Invoke-SmokeJson {
    param(
        [string]$Method = "GET",
        [string]$Uri,
        [object]$Body = $null,
        [hashtable]$Headers = @{},
        [hashtable]$ExtraHeaders = @{}
    )

    $requestHeaders = @{}
    foreach ($key in $Headers.Keys) { $requestHeaders[$key] = $Headers[$key] }
    foreach ($key in $ExtraHeaders.Keys) { $requestHeaders[$key] = $ExtraHeaders[$key] }

    $arguments = @{
        Method = $Method
        Uri = $Uri
        Headers = $requestHeaders
        TimeoutSec = 10
    }

    $requestBody = ""
    if ($null -ne $Body) {
        $requestBody = $Body | ConvertTo-Json -Depth 10
        $arguments.ContentType = "application/json"
        $arguments.Body = $requestBody
    }

    try {
        Invoke-RestMethod @arguments
    }
    catch {
        $responseBody = ""
        if ($_.Exception.Response -and $_.Exception.Response.GetResponseStream()) {
            $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            $reader.Dispose()
        }

        throw "HTTP $Method $Uri failed. Body=$requestBody Response=$responseBody"
    }
}

function Assert-HasValue {
    param(
        [object]$Value,
        [string]$Message
    )

    if ($null -eq $Value -or ([string]$Value).Length -eq 0) {
        throw $Message
    }
}

function ConvertTo-SmokeArray {
    param([object]$Value)

    @($Value | ForEach-Object { $_ })
}

Assert-InWorkspace $tmp
if (Test-Path $tmp) {
    Remove-Item -LiteralPath $tmp -Recurse -Force
}

New-Item -ItemType Directory -Path $tmp | Out-Null

$apiUrl = "http://127.0.0.1:$ApiPort"
$previousEnv = @{}
$envMap = @{
    ASPNETCORE_ENVIRONMENT = "Local"
    ASPNETCORE_URLS = $apiUrl
    "Database__Provider" = "Sqlite"
    "Database__ApplyMigrationsOnStartup" = "false"
    "Database__UseEnsureCreatedForLocalSqlite" = "true"
    "Database__SeedDemoData" = "true"
    "ConnectionStrings__DefaultConnection" = "Data Source=$tmp\vpnplatform-fresh-local-smoke.db"
    "AdminBootstrap__Enabled" = "true"
    "AdminBootstrap__Email" = "fresh-admin@example.test"
    "AdminBootstrap__Password" = "LocalSmokePassword123!"
    "Jwt__SigningKey" = "local-fresh-smoke-jwt-signing-key-64-characters-safe-for-tests"
    "Security__SecretEncryptionKey" = "local-fresh-smoke-secret-key-32-chars"
    "DataProtection__KeyPath" = (Join-Path $tmp "keys")
    "Vpn__X3Ui__Mode" = "Sandbox"
}

foreach ($key in $envMap.Keys) {
    $previousEnv[$key] = [Environment]::GetEnvironmentVariable($key, "Process")
    [Environment]::SetEnvironmentVariable($key, [string]$envMap[$key], "Process")
}

$log = Join-Path $tmp "api.out.log"
$err = Join-Path $tmp "api.err.log"
$process = $null

try {
    $listeners = @(Get-NetTCPConnection -LocalPort $ApiPort -State Listen -ErrorAction SilentlyContinue)
    if ($listeners.Count -gt 0) {
        throw "API port $ApiPort is already occupied."
    }

    $process = Start-Process -FilePath "dotnet" `
        -ArgumentList @("run", "--project", "backend\src\VpnPlatform.Api\VpnPlatform.Api.csproj", "--urls", $apiUrl) `
        -WorkingDirectory $root `
        -RedirectStandardOutput $log `
        -RedirectStandardError $err `
        -PassThru `
        -WindowStyle Hidden

    $ready = $false
    for ($attempt = 0; $attempt -lt 120; $attempt++) {
        Start-Sleep -Milliseconds 500
        if ($process.HasExited) {
            throw "API exited early with code $($process.ExitCode). StdErr: $(Get-Content -LiteralPath $err -Raw)"
        }

        try {
            $live = Invoke-SmokeJson -Uri "$apiUrl/health/live"
            $readyResponse = Invoke-SmokeJson -Uri "$apiUrl/health/ready"
            $ready = $true
            break
        }
        catch {
        }
    }

    if (-not $ready) {
        throw "API did not become ready on $apiUrl."
    }

    $tariffs = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/public/tariffs")
    if ($tariffs.Count -eq 0) {
        throw "Local seed did not expose public tariffs."
    }

    $providers = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/public/payments/providers")
    $provider = $providers | Where-Object { $_.provider -eq "YooKassa" } | Select-Object -First 1
    if ($null -eq $provider) {
        throw "Local seed did not expose YooKassa sandbox provider."
    }

    $tariff = $tariffs[0]
    $tariffId = [string]$tariff.id
    Assert-HasValue $tariffId "Public tariff does not contain id."

    $email = "fresh-user-$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())@example.test"
    $checkout = Invoke-SmokeJson -Method "POST" -Uri "$apiUrl/api/public/checkout-sessions" -Body @{
        tariffId = $tariffId
        type = "NewSubscription"
        channel = "Web"
        paymentProvider = "YooKassa"
        promoCode = $null
        isFirstPurchase = $true
        emailHint = $email
        returnUrl = "$apiUrl/local-smoke-return"
    }
    Assert-HasValue $checkout.token "Checkout session response does not contain token."

    $checkoutLookup = Invoke-SmokeJson -Uri "$apiUrl/api/public/checkout-sessions/$($checkout.token)"
    if ($checkoutLookup.status -ne "open") {
        throw "Expected checkout session status open, got $($checkoutLookup.status)."
    }

    $auth = Invoke-SmokeJson -Method "POST" -Uri "$apiUrl/api/auth/register" -Body @{
        email = $email
        password = "LocalSmokePassword123!"
        displayName = "Fresh Local User"
    }
    Assert-HasValue $auth.accessToken "Register response does not contain accessToken."
    $headers = @{ Authorization = "Bearer $($auth.accessToken)" }

    $adminAuth = Invoke-SmokeJson -Method "POST" -Uri "$apiUrl/api/auth/login" -Body @{
        email = "fresh-admin@example.test"
        password = "LocalSmokePassword123!"
    }
    Assert-HasValue $adminAuth.accessToken "Admin login response does not contain accessToken."
    $adminHeaders = @{ Authorization = "Bearer $($adminAuth.accessToken)" }

    $encodedEmail = [Uri]::EscapeDataString($email)
    $adminUsers = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/admin/users?search=$encodedEmail" -Headers $adminHeaders)
    $adminUser = $adminUsers | Where-Object { $_.email -eq $email } | Select-Object -First 1
    if ($null -eq $adminUser) {
        throw "Registered user is missing from the admin user list."
    }

    Assert-HasValue $adminUser.updatedAt "Admin user response does not contain updatedAt."
    $updatedAdminUser = Invoke-SmokeJson -Method "PATCH" -Uri "$apiUrl/api/admin/users/$($adminUser.id)" -Headers $adminHeaders -Body @{
        displayName = "Fresh Local User Updated"
        updatedAt = $adminUser.updatedAt
    }
    if ($updatedAdminUser.displayName -ne "Fresh Local User Updated") {
        throw "Admin user update did not return the updated display name."
    }

    $adminOverview = Invoke-SmokeJson -Uri "$apiUrl/api/admin/users/$($adminUser.id)/overview" -Headers $adminHeaders
    if ($adminOverview.user.displayName -ne "Fresh Local User Updated") {
        throw "Admin user overview does not contain the persisted display name."
    }

    $order = Invoke-SmokeJson -Method "POST" -Uri "$apiUrl/api/me/checkout-sessions/$($checkout.token)/claim" -Headers $headers
    Assert-HasValue $order.id "Claim response does not contain order id."

    $payment = Invoke-SmokeJson -Method "POST" -Uri "$apiUrl/api/me/orders/$($order.id)/payments/YooKassa/init" -Headers $headers -Body @{
        returnUrl = "$apiUrl/local-smoke-return"
    }
    Assert-HasValue $payment.paymentId "Payment init response does not contain paymentId."
    Assert-HasValue $payment.redirectUrl "Payment init response does not contain redirectUrl."

    $webhookBody = @{
        event = "payment.succeeded"
        object = @{
            id = $payment.paymentId
            status = "succeeded"
            paid = $true
            amount = @{
                value = ([decimal]$order.amount).ToString("0.00", [System.Globalization.CultureInfo]::InvariantCulture)
                currency = [string]$order.currency
            }
        }
    }
    $webhook = Invoke-SmokeJson -Method "POST" -Uri "$apiUrl/api/webhooks/payments/yookassa" -Body $webhookBody -ExtraHeaders @{ "X-YooKassa-Sandbox-Webhook" = "true" }
    Assert-HasValue $webhook.status "Webhook response does not contain status."

    $orders = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/me/orders" -Headers $headers)
    $payments = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/me/payments" -Headers $headers)
    $subscriptions = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/me/subscriptions" -Headers $headers)
    $accesses = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/me/accesses" -Headers $headers)
    $latest = Invoke-SmokeJson -Uri "$apiUrl/api/app-version/latest" -Headers $headers

    if ($orders.Count -eq 0 -or $payments.Count -eq 0) {
        throw "Cabinet order/payment history is empty after sandbox purchase."
    }

    if ($subscriptions.Count -eq 0) {
        throw "Sandbox purchase did not create subscription."
    }

    if ($accesses.Count -eq 0) {
        throw "Sandbox purchase did not create VPN access."
    }

    $paidPayment = $payments | Where-Object { $_.providerPaymentId -eq $payment.paymentId } | Select-Object -First 1
    if ($null -eq $paidPayment -or $paidPayment.status -ne "Succeeded" -or $paidPayment.isActivationProcessed -ne $true) {
        throw "Payment was not processed as succeeded with activation."
    }

    $activeSubscription = $subscriptions | Where-Object { $_.status -eq "Active" } | Select-Object -First 1
    if ($null -eq $activeSubscription) {
        throw "No active subscription after sandbox purchase."
    }

    $access = $accesses[0]
    Assert-HasValue $access.accessUri "VPN access does not contain accessUri."
    if (-not ([string]$access.accessUri).StartsWith("vless://", [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Unexpected access URI protocol: $($access.accessUri)"
    }

    Write-Output "fresh local smoke ok live=$($live.status) ready=$($readyResponse.status) tariffs=$($tariffs.Count) providers=$($providers.Count) adminUser=$($adminUser.id) order=$($order.id) payment=$($payment.paymentId) subscription=$($activeSubscription.id) access=$($access.id) latest=$($latest.latestRelease.releaseId)"
}
finally {
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }

    foreach ($key in $previousEnv.Keys) {
        [Environment]::SetEnvironmentVariable($key, $previousEnv[$key], "Process")
    }

    Start-Sleep -Milliseconds 500
    if (-not $KeepArtifacts) {
        Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue
        if ((Test-Path -LiteralPath $tmpRoot) -and -not (Get-ChildItem -LiteralPath $tmpRoot -Force)) {
            Remove-Item -LiteralPath $tmpRoot -Force
        }
    }
    else {
        Write-Host "Artifacts kept in $tmp"
    }
}
