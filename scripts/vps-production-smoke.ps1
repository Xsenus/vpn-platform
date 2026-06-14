param(
    [Parameter(Mandatory = $true)]
    [string]$ApiBaseUrl,
    [string]$PublicWebUrl = "",
    [string]$CabinetWebUrl = "",
    [string]$AdminWebUrl = "",
    [string]$AdminEmail = "",
    [string]$AdminPassword = "",
    [string]$PaymentProvider = "YooKassa",
    [switch]$AllowSandboxWebhook,
    [switch]$RequireWebApps
)

$ErrorActionPreference = "Stop"

function Trim-SmokeUrl {
    param([string]$Value)
    return $Value.Trim().TrimEnd("/")
}

function ConvertTo-SmokeArray {
    param([object]$Value)
    @($Value | ForEach-Object { $_ })
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
        TimeoutSec = 20
    }

    $requestBody = ""
    if ($null -ne $Body) {
        $requestBody = $Body | ConvertTo-Json -Depth 12
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

function Assert-WebApp {
    param(
        [string]$Name,
        [string]$Url
    )

    if ([string]::IsNullOrWhiteSpace($Url)) {
        if ($RequireWebApps) {
            throw "$Name URL is required when -RequireWebApps is set."
        }

        return
    }

    $response = Invoke-WebRequest -Uri (Trim-SmokeUrl $Url) -UseBasicParsing -TimeoutSec 20
    if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
        throw "$Name returned HTTP $($response.StatusCode)."
    }

    $html = [string]$response.Content
    if ($html -notmatch "(?is)<!doctype html|<div[^>]+id=[`"']root[`"']|<script[^>]+type=[`"']module[`"']") {
        throw "$Name response does not look like a frontend SPA entrypoint."
    }
}

$apiUrl = Trim-SmokeUrl $ApiBaseUrl
if ([string]::IsNullOrWhiteSpace($apiUrl)) {
    throw "ApiBaseUrl is required."
}

$live = Invoke-SmokeJson -Uri "$apiUrl/health/live"
$ready = Invoke-SmokeJson -Uri "$apiUrl/health/ready"
if ($live.status -ne "ok") {
    throw "Expected live status ok, got $($live.status)."
}

if ($ready.status -ne "Ready") {
    throw "Expected ready status Ready, got $($ready.status)."
}

Assert-WebApp -Name "Public web" -Url $PublicWebUrl
Assert-WebApp -Name "Cabinet web" -Url $CabinetWebUrl
Assert-WebApp -Name "Admin web" -Url $AdminWebUrl

if (-not [string]::IsNullOrWhiteSpace($AdminEmail) -or -not [string]::IsNullOrWhiteSpace($AdminPassword)) {
    Assert-HasValue $AdminEmail "AdminEmail is required when admin login smoke is enabled."
    Assert-HasValue $AdminPassword "AdminPassword is required when admin login smoke is enabled."

    $adminAuth = Invoke-SmokeJson -Method "POST" -Uri "$apiUrl/api/auth/login" -Body @{
        email = $AdminEmail
        password = $AdminPassword
    }
    Assert-HasValue $adminAuth.accessToken "Admin login response does not contain accessToken."

    $adminHeaders = @{ Authorization = "Bearer $($adminAuth.accessToken)" }
    $adminSummary = Invoke-SmokeJson -Uri "$apiUrl/api/admin/dashboard/summary" -Headers $adminHeaders
    Assert-HasValue $adminSummary.generatedAt "Admin dashboard summary does not contain generatedAt."
}

$tariffs = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/public/tariffs")
if ($tariffs.Count -eq 0) {
    throw "Public tariffs endpoint returned empty list."
}

$providers = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/public/payments/providers")
$provider = $providers | Where-Object { $_.provider -eq $PaymentProvider } | Select-Object -First 1
if ($null -eq $provider) {
    throw "Public payment providers do not include $PaymentProvider."
}

$tariff = $tariffs[0]
$tariffId = [string]$tariff.id
Assert-HasValue $tariffId "Public tariff does not contain id."

$email = "vps-smoke-$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())@example.test"
$checkout = Invoke-SmokeJson -Method "POST" -Uri "$apiUrl/api/public/checkout-sessions" -Body @{
    tariffId = $tariffId
    type = "NewSubscription"
    channel = "Web"
    paymentProvider = $PaymentProvider
    promoCode = $null
    isFirstPurchase = $true
    emailHint = $email
    returnUrl = "$apiUrl/vps-smoke-return"
}
Assert-HasValue $checkout.token "Checkout session response does not contain token."

$checkoutLookup = Invoke-SmokeJson -Uri "$apiUrl/api/public/checkout-sessions/$($checkout.token)"
if ($checkoutLookup.status -ne "open") {
    throw "Expected checkout session status open, got $($checkoutLookup.status)."
}

$auth = Invoke-SmokeJson -Method "POST" -Uri "$apiUrl/api/auth/register" -Body @{
    email = $email
    password = "VpsSmokePassword123!"
    displayName = "VPS Smoke User"
}
Assert-HasValue $auth.accessToken "Register response does not contain accessToken."
$headers = @{ Authorization = "Bearer $($auth.accessToken)" }

$order = Invoke-SmokeJson -Method "POST" -Uri "$apiUrl/api/me/checkout-sessions/$($checkout.token)/claim" -Headers $headers
Assert-HasValue $order.id "Claim response does not contain order id."

$payment = Invoke-SmokeJson -Method "POST" -Uri "$apiUrl/api/me/orders/$($order.id)/payments/$PaymentProvider/init" -Headers $headers -Body @{
    returnUrl = "$apiUrl/vps-smoke-return"
}
Assert-HasValue $payment.paymentId "Payment init response does not contain paymentId."
Assert-HasValue $payment.redirectUrl "Payment init response does not contain redirectUrl."

if (-not $AllowSandboxWebhook) {
    Write-Output "vps production smoke partial ok live=$($live.status) ready=$($ready.status) tariffs=$($tariffs.Count) providers=$($providers.Count) order=$($order.id) payment=$($payment.paymentId) next=complete-provider-payment-or-run-with-AllowSandboxWebhook-on-non-production-sandbox"
    return
}

if ([string]$live.environment -eq "Production") {
    throw "-AllowSandboxWebhook is forbidden when /health/live reports Production."
}

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

$webhookHeaders = @{}
if ($PaymentProvider -eq "YooKassa") {
    $webhookHeaders["X-YooKassa-Sandbox-Webhook"] = "true"
}

$providerRoute = $PaymentProvider.ToLowerInvariant()
$webhook = Invoke-SmokeJson -Method "POST" -Uri "$apiUrl/api/webhooks/payments/$providerRoute" -Body $webhookBody -ExtraHeaders $webhookHeaders
Assert-HasValue $webhook.status "Webhook response does not contain status."

$orders = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/me/orders" -Headers $headers)
$payments = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/me/payments" -Headers $headers)
$subscriptions = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/me/subscriptions" -Headers $headers)
$accesses = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/me/accesses" -Headers $headers)
$latest = Invoke-SmokeJson -Uri "$apiUrl/api/app-version/latest" -Headers $headers

if ($orders.Count -eq 0 -or $payments.Count -eq 0) {
    throw "Cabinet order/payment history is empty after smoke purchase."
}

$paidPayment = $payments | Where-Object { $_.providerPaymentId -eq $payment.paymentId } | Select-Object -First 1
if ($null -eq $paidPayment -or $paidPayment.status -ne "Succeeded" -or $paidPayment.isActivationProcessed -ne $true) {
    throw "Payment was not processed as succeeded with activation."
}

$activeSubscription = $subscriptions | Where-Object { $_.status -eq "Active" } | Select-Object -First 1
if ($null -eq $activeSubscription) {
    throw "No active subscription after smoke purchase."
}

if ($accesses.Count -eq 0) {
    throw "Smoke purchase did not create VPN access."
}

$access = $accesses[0]
Assert-HasValue $access.accessUri "VPN access does not contain accessUri."
if (-not ([string]$access.accessUri).StartsWith("vless://", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Unexpected access URI protocol: $($access.accessUri)"
}

Write-Output "vps production smoke ok live=$($live.status) ready=$($ready.status) tariffs=$($tariffs.Count) providers=$($providers.Count) order=$($order.id) payment=$($payment.paymentId) subscription=$($activeSubscription.id) access=$($access.id) latest=$($latest.latestRelease.releaseId)"
