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
        $responseBody = [string]$_.ErrorDetails.Message
        if (-not $responseBody -and $_.Exception.Response -and $_.Exception.Response.Content) {
            try {
                $responseBody = $_.Exception.Response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            }
            catch {
                $responseBody = "<response body unavailable>"
            }
        }
        elseif (-not $responseBody -and $_.Exception.Response -and $_.Exception.Response.PSObject.Methods.Name -contains "GetResponseStream" -and $_.Exception.Response.GetResponseStream()) {
            $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            $reader.Dispose()
        }

        throw "HTTP $Method $Uri failed. Body=$requestBody Response=$responseBody"
    }
}

function Assert-SmokeJsonStatus {
    param(
        [string]$Method,
        [string]$Uri,
        [object]$Body,
        [hashtable]$Headers,
        [int]$ExpectedStatus
    )

    $requestBody = $Body | ConvertTo-Json -Depth 10
    $requestBodyBytes = [System.Text.Encoding]::UTF8.GetBytes($requestBody)
    try {
        Invoke-RestMethod -Method $Method -Uri $Uri -Headers $Headers -ContentType "application/json; charset=utf-8" -Body $requestBodyBytes -TimeoutSec 10 | Out-Null
        throw "HTTP $Method $Uri unexpectedly succeeded; expected status $ExpectedStatus."
    }
    catch {
        $actualStatus = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        if ($actualStatus -ne $ExpectedStatus) {
            throw "HTTP $Method $Uri returned status $actualStatus; expected $ExpectedStatus."
        }
    }

    return $ExpectedStatus
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

    $siteContentBlocks = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/admin/site-content?group=home" -Headers $adminHeaders)
    $siteContentBlock = $siteContentBlocks | Select-Object -First 1
    if ($null -eq $siteContentBlock) {
        throw "Managed site content seed is missing."
    }

    $managedNoOpStatus = Assert-SmokeJsonStatus -Method "PUT" -Uri "$apiUrl/api/admin/site-content/$($siteContentBlock.id)" -Headers $adminHeaders -ExpectedStatus 400 -Body @{
        key = $siteContentBlock.key
        value = $siteContentBlock.value
        group = $siteContentBlock.group
        label = $siteContentBlock.label
        description = $siteContentBlock.description
        inputType = $siteContentBlock.inputType
        isActive = $siteContentBlock.isActive
        sortOrder = $siteContentBlock.sortOrder
        revision = $siteContentBlock.revision
    }

    $adminTariffs = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/admin/tariffs" -Headers $adminHeaders)
    $adminTariff = $adminTariffs | Select-Object -First 1
    if ($null -eq $adminTariff) {
        throw "Admin tariff seed is missing."
    }
    $tariffNoOpStatus = Assert-SmokeJsonStatus -Method "PATCH" -Uri "$apiUrl/api/admin/tariffs/$($adminTariff.id)" -Headers $adminHeaders -ExpectedStatus 400 -Body @{
        name = $adminTariff.name
        revision = $adminTariff.revision
    }

    $referralPrograms = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/admin/referral-programs" -Headers $adminHeaders)
    $referralProgram = $referralPrograms | Select-Object -First 1
    if ($null -eq $referralProgram) {
        $referralProgram = Invoke-SmokeJson -Method "POST" -Uri "$apiUrl/api/admin/referral-programs" -Headers $adminHeaders -Body @{
            name = "Fresh Local Referral"
            status = "draft"
            startAt = $null
            endAt = $null
            ruleDefinition = '{"firstPurchaseOnly":true,"minimumOrderAmount":0,"allowedChannels":["Web"]}'
            rewardDefinition = '{"referrer":{"type":"bonus-days","value":7,"unit":"days","autoApprove":true}}'
            antiFraudSettings = '{}'
        }
    }
    $referralNoOpStatus = Assert-SmokeJsonStatus -Method "PATCH" -Uri "$apiUrl/api/admin/referral-programs/$($referralProgram.id)" -Headers $adminHeaders -ExpectedStatus 400 -Body @{
        name = $referralProgram.name
        status = $referralProgram.status
        startAt = $referralProgram.startAt
        endAt = $referralProgram.endAt
        ruleDefinition = $referralProgram.ruleDefinition
        rewardDefinition = $referralProgram.rewardDefinition
        antiFraudSettings = $referralProgram.antiFraudSettings
        revision = $referralProgram.revision
    }

    $adminReleases = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/app-version/admin/releases" -Headers $adminHeaders)
    $adminRelease = $adminReleases | Select-Object -First 1
    if ($null -eq $adminRelease) {
        throw "App release seed is missing."
    }
    $releaseNoOpStatus = Assert-SmokeJsonStatus -Method "PUT" -Uri "$apiUrl/api/app-version/admin/releases/$($adminRelease.id)" -Headers $adminHeaders -ExpectedStatus 400 -Body @{
        releaseId = $adminRelease.releaseId
        version = $adminRelease.version
        releasedAt = $adminRelease.releasedAt
        title = $adminRelease.title
        summary = $adminRelease.summary
        isActive = $adminRelease.isActive
        source = $adminRelease.source
        items = $adminRelease.items
        revision = $adminRelease.revision
    }

    $providerAccounts = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/admin/payment-providers/accounts" -Headers $adminHeaders)
    $providerAccount = $providerAccounts | Select-Object -First 1
    if ($null -eq $providerAccount) {
        throw "Payment provider account seed is missing."
    }
    $providerAccountBody = @{
        provider = $providerAccount.provider
        mode = $providerAccount.mode
        name = $providerAccount.name
        publicName = $providerAccount.publicName
        isEnabled = $providerAccount.isEnabled
        isDefault = $providerAccount.isDefault
        shopId = $providerAccount.shopId
        apiBaseUrl = $providerAccount.apiBaseUrl
        returnUrl = $providerAccount.returnUrl
        webhookUrl = $providerAccount.webhookUrl
        secretKey = ""
        webhookSecret = ""
        useWebhookIpAllowList = $providerAccount.useWebhookIpAllowList
        allowedWebhookIpRangesCsv = $providerAccount.allowedWebhookIpRangesCsv
        extraSettingsJson = ""
        revision = $providerAccount.revision
    }
    $providerNoOpStatus = Assert-SmokeJsonStatus -Method "PATCH" -Uri "$apiUrl/api/admin/payment-providers/accounts/$($providerAccount.id)" -Headers $adminHeaders -ExpectedStatus 400 -Body $providerAccountBody
    $providerStateNoOpStatus = Assert-SmokeJsonStatus -Method "POST" -Uri "$apiUrl/api/admin/payment-providers/accounts/$($providerAccount.id)/enabled" -Headers $adminHeaders -ExpectedStatus 400 -Body @{
        enabled = $providerAccount.isEnabled
        revision = $providerAccount.revision
    }
    $staleProviderBody = @{} + $providerAccountBody
    $staleProviderBody.publicName = "$($providerAccount.publicName) stale"
    $staleProviderBody.revision = [int]$providerAccount.revision + 1
    $providerConflictStatus = Assert-SmokeJsonStatus -Method "PATCH" -Uri "$apiUrl/api/admin/payment-providers/accounts/$($providerAccount.id)" -Headers $adminHeaders -ExpectedStatus 409 -Body $staleProviderBody

    $adminServers = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/admin/servers" -Headers $adminHeaders)
    $adminServer = $adminServers | Select-Object -First 1
    if ($null -eq $adminServer) {
        throw "Admin VPN server seed is missing."
    }
    $serverValidationMode = [string]$adminServer.tagsCsv -match '(^|,)validation-mode:true(,|$)'
    $serverNoOpStatus = Assert-SmokeJsonStatus -Method "PUT" -Uri "$apiUrl/api/admin/servers/$($adminServer.id)" -Headers $adminHeaders -ExpectedStatus 400 -Body @{
        name = $adminServer.name
        host = $adminServer.host
        ipAddress = $adminServer.ipAddress
        provider = $adminServer.provider
        region = $adminServer.region
        country = $adminServer.country
        datacenter = $adminServer.datacenter
        capacity = $adminServer.capacity
        supportedProtocolsCsv = $adminServer.supportedProtocolsCsv
        priority = $adminServer.priority
        tagsCsv = $adminServer.tagsCsv
        sshUser = $adminServer.sshUser
        sshPort = $adminServer.sshPort
        sshPrivateKeyPath = ""
        sshAuthMethod = $adminServer.sshAuthMethod
        sshCredential = ""
        validationMode = $serverValidationMode
        ownerType = $null
        skipHostKeyChecking = $adminServer.skipHostKeyChecking
        panelBaseUrl = $adminServer.panelBaseUrl
        panelUsername = $adminServer.panelUsername
        panelPassword = ""
        panelInboundId = $adminServer.panelInboundId
        publicHostname = $adminServer.publicHostname
        publicPort = $adminServer.publicPort
        nodeGroupId = $adminServer.nodeGroupId
        revision = $adminServer.revision
    }
    $serverStateNoOpStatus = Assert-SmokeJsonStatus -Method "POST" -Uri "$apiUrl/api/admin/servers/$($adminServer.id)/disable-maintenance" -Headers $adminHeaders -ExpectedStatus 400 -Body @{
        revision = $adminServer.revision
    }

    $vpnPanels = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/admin/vpn-panels" -Headers $adminHeaders)
    $vpnPanel = $vpnPanels | Select-Object -First 1
    if ($null -eq $vpnPanel) {
        throw "VPN panel seed is missing."
    }
    $vpnPanelNoOpStatus = Assert-SmokeJsonStatus -Method "PATCH" -Uri "$apiUrl/api/admin/vpn-panels/$($vpnPanel.id)" -Headers $adminHeaders -ExpectedStatus 400 -Body @{
        name = $vpnPanel.name
        baseUrl = $vpnPanel.baseUrl
        login = $vpnPanel.login
        password = ""
        region = $vpnPanel.region
        capacity = $vpnPanel.capacity
        sslVerificationMode = $vpnPanel.sslVerificationMode
        apiVariant = $vpnPanel.apiVariant
        autoCreateInbound = $vpnPanel.autoCreateInbound
        defaultInboundTemplateJson = $vpnPanel.defaultInboundTemplateJson
        revision = $vpnPanel.revision
    }

    $vpnInbounds = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/admin/vpn-inbounds" -Headers $adminHeaders)
    $vpnInbound = $vpnInbounds | Select-Object -First 1
    if ($null -eq $vpnInbound) {
        throw "VPN inbound seed is missing."
    }
    $vpnInboundNoOpStatus = Assert-SmokeJsonStatus -Method "PATCH" -Uri "$apiUrl/api/admin/vpn-inbounds/$($vpnInbound.id)" -Headers $adminHeaders -ExpectedStatus 400 -Body @{
        name = $vpnInbound.name
        protocol = $vpnInbound.protocol
        port = $vpnInbound.port
        listen = $vpnInbound.listen
        settingsJson = $vpnInbound.settingsJson
        streamSettingsJson = $vpnInbound.streamSettingsJson
        sniffingJson = $vpnInbound.sniffingJson
        isDefault = $vpnInbound.isDefault
        capacity = $vpnInbound.capacity
        isActive = $vpnInbound.isActive
        revision = $vpnInbound.revision
    }

    $telegramSettings = Invoke-SmokeJson -Uri "$apiUrl/api/admin/telegram-bot/settings" -Headers $adminHeaders
    if ($telegramSettings.revision -lt 0) {
        throw "Telegram bot settings response does not contain a valid revision."
    }

    $telegramWelcome = "Fresh local Telegram welcome"
    $updatedTelegramSettings = Invoke-SmokeJson -Method "PATCH" -Uri "$apiUrl/api/admin/telegram-bot/settings" -Headers $adminHeaders -Body @{
        welcomeText = $telegramWelcome
        revision = $telegramSettings.revision
    }
    if ($updatedTelegramSettings.welcomeText -ne $telegramWelcome) {
        throw "Telegram bot settings update did not persist the welcome text."
    }

    if ($updatedTelegramSettings.revision -ne ($telegramSettings.revision + 1)) {
        throw "Telegram bot settings revision did not advance after the update."
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

    $archiveResult = Invoke-SmokeJson -Method "DELETE" -Uri "$apiUrl/api/admin/servers/$($adminServer.id)?revision=$($adminServer.revision)" -Headers $adminHeaders
    if ($archiveResult.archived -ne $true -or $archiveResult.deleted -ne $false) {
        throw "Linked VPN server was not archived by the first delete command."
    }
    $archivedServers = ConvertTo-SmokeArray (Invoke-SmokeJson -Uri "$apiUrl/api/admin/servers" -Headers $adminHeaders)
    $archivedServer = $archivedServers | Where-Object { $_.id -eq $adminServer.id } | Select-Object -First 1
    if ($null -eq $archivedServer -or $archivedServer.status -ne "Archived") {
        throw "Archived VPN server was not returned by the admin list."
    }
    $serverArchiveNoOpStatus = Assert-SmokeJsonStatus -Method "DELETE" -Uri "$apiUrl/api/admin/servers/$($archivedServer.id)?revision=$($archivedServer.revision)" -Headers $adminHeaders -ExpectedStatus 400 -Body @{}

    Write-Output "fresh local smoke ok live=$($live.status) ready=$($readyResponse.status) tariffs=$($tariffs.Count) providers=$($providers.Count) adminUser=$($adminUser.id) managedNoOp=$managedNoOpStatus commerceNoOps=$tariffNoOpStatus,$referralNoOpStatus,$releaseNoOpStatus providerGuards=$providerNoOpStatus,$providerStateNoOpStatus,$providerConflictStatus serverGuards=$serverNoOpStatus,$serverStateNoOpStatus serverArchiveNoOp=$serverArchiveNoOpStatus vpnNoOps=$vpnPanelNoOpStatus,$vpnInboundNoOpStatus telegramRevision=$($updatedTelegramSettings.revision) order=$($order.id) payment=$($payment.paymentId) subscription=$($activeSubscription.id) access=$($access.id) latest=$($latest.latestRelease.releaseId)"
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
