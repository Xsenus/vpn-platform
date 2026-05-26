using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Security;
using static VpnPlatform.Infrastructure.Payments.PaymentProviderShared;

namespace VpnPlatform.Infrastructure.Payments;

public abstract class UnsupportedPaymentProviderBase : IPaymentProvider, IPaymentStatusMapper, IPaymentWebhookVerifier
{
    public abstract PaymentProvider Provider { get; }

    public Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
        => throw new NotSupportedException($"{Provider} adapter is not implemented. Keep this provider disabled until its production adapter is added.");

    public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
        => throw new NotSupportedException($"{Provider} webhook parser is not implemented.");

    public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
        => throw new NotSupportedException($"{Provider} status recheck is not implemented.");

    public Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken)
        => throw new NotSupportedException($"{Provider} refund adapter is not implemented.");

    public PaymentStatus MapPaymentStatus(string providerStatus, bool paid) => PaymentStatus.Unknown;
    public RefundStatus MapRefundStatus(string providerStatus) => RefundStatus.Unknown;

    public Task<PaymentWebhookVerificationResult> VerifyAsync(PaymentProviderAccount account, PaymentWebhookParseResult parsed, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
        => Task.FromResult(new PaymentWebhookVerificationResult(false, "unsupported", $"{Provider} webhook verification is not implemented."));
}

public sealed class TelegramStarsPaymentProvider : UnsupportedPaymentProviderBase { public override PaymentProvider Provider => PaymentProvider.TelegramStars; }

public static class YooKassaPaymentStatusMapper
{
    public static PaymentStatus MapPaymentStatus(string providerStatus, bool paid)
        => providerStatus.Trim().ToLowerInvariant() switch
        {
            "pending" => PaymentStatus.Pending,
            "waiting_for_capture" => PaymentStatus.WaitingConfirmation,
            "succeeded" when paid => PaymentStatus.Succeeded,
            "succeeded" => PaymentStatus.Unknown,
            "canceled" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Unknown
        };

    public static RefundStatus MapRefundStatus(string providerStatus)
        => providerStatus.Trim().ToLowerInvariant() switch
        {
            "pending" => RefundStatus.Pending,
            "succeeded" => RefundStatus.Succeeded,
            "canceled" => RefundStatus.Cancelled,
            _ => RefundStatus.Unknown
        };
}

public static class RoboKassaPaymentStatusMapper
{
    public static PaymentStatus MapPaymentStatus(string providerStatus, bool paid)
        => providerStatus.Trim().ToUpperInvariant() switch
        {
            "OK" => PaymentStatus.Succeeded,
            "COMPLETED" => PaymentStatus.Succeeded,
            "SUCCESS" => PaymentStatus.Succeeded,
            _ when paid => PaymentStatus.Succeeded,
            _ => PaymentStatus.Unknown
        };
}

public static class YooMoneyPaymentStatusMapper
{
    public static PaymentStatus MapPaymentStatus(bool codepro, bool unaccepted)
        => !codepro && !unaccepted ? PaymentStatus.Succeeded : PaymentStatus.Pending;
}

public sealed class YooKassaPaymentProvider : IPaymentProvider, IPaymentWebhookVerifier, IPaymentStatusMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };
    private static readonly string[] DefaultWebhookCidrs =
    {
        "185.71.76.0/27",
        "185.71.77.0/27",
        "77.75.153.0/25",
        "77.75.156.11/32",
        "77.75.156.35/32",
        "77.75.154.128/25",
        "2a02:5180::/32"
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PaymentProviderAccountService _accounts;
    private readonly ILogger<YooKassaPaymentProvider> _logger;
    private readonly IHostEnvironment _environment;

    public YooKassaPaymentProvider(IHttpClientFactory httpClientFactory, PaymentProviderAccountService accounts, ILogger<YooKassaPaymentProvider> logger, IHostEnvironment environment)
    {
        _httpClientFactory = httpClientFactory;
        _accounts = accounts;
        _logger = logger;
        _environment = environment;
    }

    public PaymentProvider Provider => PaymentProvider.YooKassa;

    public async Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
    {
        EnsureEnabled(request.Account, Provider);

        if (IsLocalSandboxEnvironment() && request.Account.Mode == PaymentProviderMode.Sandbox && IsLocalSandboxWithoutCredentials(request.Account))
        {
            var sandboxPaymentId = $"yk_sandbox_{request.Payment.Id:N}";
            var sandboxUrl = AppendQuery(request.ReturnUrl, new Dictionary<string, string> { ["paymentId"] = sandboxPaymentId, ["sandbox"] = "1", ["provider"] = "YooKassa" });
            request.Payment.RawRequest = JsonSerializer.Serialize(new
            {
                provider = "YooKassa",
                mode = "Sandbox",
                orderId = request.Order.Id,
                paymentAttemptId = request.Payment.Id,
                amount = request.Order.Amount,
                currency = request.Order.Currency,
                returnUrl = request.ReturnUrl
            }, JsonOptions);
            var sandboxRaw = JsonSerializer.Serialize(new
            {
                id = sandboxPaymentId,
                status = "pending",
                paid = false,
                test = true,
                amount = new { value = request.Order.Amount.ToString("0.00", CultureInfo.InvariantCulture), currency = request.Order.Currency },
                confirmation = new { type = "redirect", confirmation_url = sandboxUrl },
                metadata = new { orderId = request.Order.Id, paymentAttemptId = request.Payment.Id }
            }, JsonOptions);
            return new PaymentInitResult(sandboxPaymentId, sandboxUrl, sandboxRaw);
        }

        var secretKey = _accounts.GetSecretKey(request.Account);
        if (string.IsNullOrWhiteSpace(request.Account.ShopId) || string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("YooKassa ShopId and SecretKey are required.");
        }

        var body = new JsonObject
        {
            ["amount"] = new JsonObject
            {
                ["value"] = request.Order.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                ["currency"] = request.Order.Currency
            },
            ["capture"] = true,
            ["confirmation"] = new JsonObject
            {
                ["type"] = "redirect",
                ["return_url"] = request.ReturnUrl
            },
            ["description"] = BuildDescription(request.Order),
            ["metadata"] = new JsonObject
            {
                ["orderId"] = request.Order.Id.ToString(),
                ["paymentAttemptId"] = request.Payment.Id.ToString(),
                ["userId"] = request.Order.UserId.ToString(),
                ["providerAccountId"] = request.Account.Id.ToString()
            }
        };

        var rawRequest = body.ToJsonString(JsonOptions);
        request.Payment.RawRequest = rawRequest;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildUri(request.Account, "payments"));
        httpRequest.Headers.Authorization = BuildBasicAuth(request.Account.ShopId, secretKey);
        httpRequest.Headers.Add("Idempotence-Key", request.Payment.IdempotencyKey);
        httpRequest.Content = new StringContent(rawRequest, Encoding.UTF8, "application/json");

        using var response = await SendAsync(httpRequest, cancellationToken);
        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("YooKassa create payment failed with status {StatusCode}. Body={Body}", (int)response.StatusCode, SecretRedactor.Redact(rawResponse));
            throw new InvalidOperationException($"YooKassa create payment failed with HTTP {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(rawResponse);
        var root = document.RootElement;
        var paymentId = root.GetProperty("id").GetString() ?? string.Empty;
        var confirmationUrl = root.TryGetProperty("confirmation", out var confirmation)
            && confirmation.TryGetProperty("confirmation_url", out var url)
                ? url.GetString() ?? string.Empty
                : string.Empty;

        if (string.IsNullOrWhiteSpace(paymentId) || string.IsNullOrWhiteSpace(confirmationUrl))
        {
            throw new InvalidOperationException("YooKassa response does not contain payment id or confirmation_url.");
        }

        return new PaymentInitResult(paymentId, confirmationUrl, rawResponse);
    }

    public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(rawBody) ? "{}" : rawBody);
        var root = document.RootElement;
        var eventType = root.TryGetProperty("event", out var eventElement) ? eventElement.GetString() ?? string.Empty : string.Empty;
        var obj = root.TryGetProperty("object", out var objectElement) ? objectElement : root;
        var paymentId = obj.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? string.Empty : string.Empty;
        var providerStatus = obj.TryGetProperty("status", out var statusElement) ? statusElement.GetString() ?? string.Empty : string.Empty;
        var paid = obj.TryGetProperty("paid", out var paidElement) && paidElement.ValueKind == JsonValueKind.True;
        var internalStatus = MapPaymentStatus(providerStatus, paid);
        var amount = TryReadAmount(obj, out var currency);
        var externalEventId = string.IsNullOrWhiteSpace(eventType) ? $"payment.{providerStatus}:{paymentId}" : $"{eventType}:{paymentId}";

        return Task.FromResult(new PaymentWebhookParseResult(externalEventId, eventType, paymentId, internalStatus, rawBody, false, amount, currency, paid));
    }

    public async Task<PaymentWebhookVerificationResult> VerifyAsync(PaymentProviderAccount account, PaymentWebhookParseResult parsed, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        if (_environment.IsProduction() && headers.TryGetValue("X-YooKassa-Sandbox-Webhook", out var productionSandboxMarker) && string.Equals(productionSandboxMarker, "true", StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentWebhookVerificationResult(false, "production-sandbox-header-denied", "Local sandbox webhook header is forbidden in production.");
        }

        if (IsLocalSandboxEnvironment() && account.Mode == PaymentProviderMode.Sandbox && IsLocalSandboxWithoutCredentials(account))
        {
            if (headers.TryGetValue("X-YooKassa-Sandbox-Webhook", out var marker) && string.Equals(marker, "true", StringComparison.OrdinalIgnoreCase))
            {
                return new PaymentWebhookVerificationResult(true, "local-sandbox-header", null);
            }

            return new PaymentWebhookVerificationResult(false, "local-sandbox-header", "Missing X-YooKassa-Sandbox-Webhook=true header.");
        }

        if (account.Mode == PaymentProviderMode.Production && headers.TryGetValue("X-YooKassa-Sandbox-Webhook", out var sandboxMarker) && string.Equals(sandboxMarker, "true", StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentWebhookVerificationResult(false, "production-sandbox-header-denied", "Local sandbox webhook header is forbidden for production provider accounts.");
        }

        if (account.UseWebhookIpAllowList)
        {
            var sourceIp = ResolveSourceIp(headers);
            if (string.IsNullOrWhiteSpace(sourceIp) || !IsAllowedWebhookIp(sourceIp, account.AllowedWebhookIpRangesCsv))
            {
                return new PaymentWebhookVerificationResult(false, "ip-allow-list", "Webhook source IP is not in YooKassa allow-list.");
            }
        }

        var paymentStatus = await RecheckPaymentStatusAsync(parsed.PaymentId, account, cancellationToken);
        if (paymentStatus.Status == PaymentStatus.Unknown)
        {
            return new PaymentWebhookVerificationResult(false, "status-recheck", "Unable to verify YooKassa payment status.");
        }

        if (paymentStatus.Status != parsed.Status)
        {
            return new PaymentWebhookVerificationResult(false, "status-recheck", $"Webhook status {parsed.Status} does not match YooKassa status {paymentStatus.Status}.");
        }

        return new PaymentWebhookVerificationResult(true, account.UseWebhookIpAllowList ? "ip-allow-list+status-recheck" : "status-recheck", null);
    }

    public async Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
    {
        if (IsLocalSandboxEnvironment() && account.Mode == PaymentProviderMode.Sandbox && IsLocalSandboxWithoutCredentials(account))
        {
            return new PaymentStatusResult(payment.ProviderPaymentId, payment.Status, payment.RawResponse, payment.StatusReason);
        }

        return await RecheckPaymentStatusAsync(payment.ProviderPaymentId, account, cancellationToken);
    }

    public async Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken)
    {
        if (payment.Status != PaymentStatus.Succeeded && payment.Status != PaymentStatus.PartiallyRefunded)
        {
            throw new InvalidOperationException("Only succeeded YooKassa payments can be refunded.");
        }

        if (IsLocalSandboxEnvironment() && account.Mode == PaymentProviderMode.Sandbox && IsLocalSandboxWithoutCredentials(account))
        {
            var sandboxRefundId = $"rf_sandbox_{ComputeSha256Hex($"{payment.Id:N}:{amount.ToString("0.00", CultureInfo.InvariantCulture)}:{reason}")[..24]}";
            var sandboxRaw = JsonSerializer.Serialize(new
            {
                id = sandboxRefundId,
                status = "succeeded",
                amount = new { value = amount.ToString("0.00", CultureInfo.InvariantCulture), currency = payment.Currency }
            }, JsonOptions);
            return new PaymentRefundResult(sandboxRefundId, RefundStatus.Succeeded, sandboxRaw);
        }

        var secretKey = _accounts.GetSecretKey(account);
        var idempotencyKey = $"refund-{payment.Id:N}-{amount.ToString("0.00", CultureInfo.InvariantCulture)}-{ComputeSha256Hex(reason)[..16]}";
        var body = new JsonObject
        {
            ["amount"] = new JsonObject
            {
                ["value"] = amount.ToString("0.00", CultureInfo.InvariantCulture),
                ["currency"] = payment.Currency
            },
            ["payment_id"] = payment.ProviderPaymentId,
            ["description"] = string.IsNullOrWhiteSpace(reason) ? $"Refund for order {payment.OrderId}" : reason
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(account, "refunds"));
        request.Headers.Authorization = BuildBasicAuth(account.ShopId, secretKey);
        request.Headers.Add("Idempotence-Key", idempotencyKey);
        request.Content = new StringContent(body.ToJsonString(JsonOptions), Encoding.UTF8, "application/json");

        using var response = await SendAsync(request, cancellationToken);
        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("YooKassa refund failed with status {StatusCode}. Body={Body}", (int)response.StatusCode, SecretRedactor.Redact(rawResponse));
            throw new InvalidOperationException($"YooKassa refund failed with HTTP {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(rawResponse);
        var root = document.RootElement;
        var refundId = root.GetProperty("id").GetString() ?? string.Empty;
        var statusText = root.TryGetProperty("status", out var statusElement) ? statusElement.GetString() ?? string.Empty : string.Empty;
        return new PaymentRefundResult(refundId, MapRefundStatus(statusText), rawResponse);
    }

    public PaymentStatus MapPaymentStatus(string providerStatus, bool paid) => YooKassaPaymentStatusMapper.MapPaymentStatus(providerStatus, paid);
    public RefundStatus MapRefundStatus(string providerStatus) => YooKassaPaymentStatusMapper.MapRefundStatus(providerStatus);

    private async Task<PaymentStatusResult> RecheckPaymentStatusAsync(string paymentId, PaymentProviderAccount account, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            return new PaymentStatusResult(paymentId, PaymentStatus.Unknown, "{}", "missing_payment_id");
        }

        var secretKey = _accounts.GetSecretKey(account);
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(account, $"payments/{WebUtility.UrlEncode(paymentId)}"));
        request.Headers.Authorization = BuildBasicAuth(account.ShopId, secretKey);

        using var response = await SendAsync(request, cancellationToken);
        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("YooKassa status recheck failed with status {StatusCode}. Body={Body}", (int)response.StatusCode, SecretRedactor.Redact(rawResponse));
            return new PaymentStatusResult(paymentId, PaymentStatus.Unknown, rawResponse, $"http_{(int)response.StatusCode}");
        }

        using var document = JsonDocument.Parse(rawResponse);
        var root = document.RootElement;
        var providerStatus = root.TryGetProperty("status", out var statusElement) ? statusElement.GetString() ?? string.Empty : string.Empty;
        var paid = root.TryGetProperty("paid", out var paidElement) && paidElement.ValueKind == JsonValueKind.True;
        return new PaymentStatusResult(paymentId, MapPaymentStatus(providerStatus, paid), rawResponse, providerStatus);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_httpClientFactory is null)
        {
            throw new InvalidOperationException("HttpClientFactory is required for live YooKassa calls.");
        }

        var client = _httpClientFactory.CreateClient("YooKassa");
        return await client.SendAsync(request, cancellationToken);
    }

    private static Uri BuildUri(PaymentProviderAccount account, string path)
    {
        var baseUrl = string.IsNullOrWhiteSpace(account.ApiBaseUrl) ? "https://api.yookassa.ru/v3" : account.ApiBaseUrl.TrimEnd('/');
        return new Uri($"{baseUrl}/{path.TrimStart('/')}", UriKind.Absolute);
    }

    private static AuthenticationHeaderValue BuildBasicAuth(string shopId, string secretKey)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{shopId}:{secretKey}"));
        return new AuthenticationHeaderValue("Basic", credentials);
    }

    private static string BuildDescription(Order order)
    {
        var value = $"VPN subscription order {order.Id}";
        return value.Length <= 128 ? value : value[..128];
    }

    private bool IsLocalSandboxEnvironment()
        => _environment.IsDevelopment()
            || _environment.IsEnvironment("Local")
            || _environment.IsEnvironment("Test")
            || _environment.IsEnvironment("Testing")
            || _environment.IsEnvironment("Sandbox");

    private static bool IsLocalSandboxWithoutCredentials(PaymentProviderAccount account)
        => account.Mode == PaymentProviderMode.Sandbox && string.IsNullOrWhiteSpace(account.SecretKeyProtected);

    private static decimal? TryReadAmount(JsonElement obj, out string? currency)
    {
        currency = null;
        if (!obj.TryGetProperty("amount", out var amountObj))
        {
            return null;
        }

        currency = amountObj.TryGetProperty("currency", out var currencyElement) ? currencyElement.GetString() : null;
        var amountText = amountObj.TryGetProperty("value", out var valueElement) ? valueElement.GetString() : null;
        return decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) ? amount : null;
    }

    private static string ResolveSourceIp(IReadOnlyDictionary<string, string> headers)
    {
        if (headers.TryGetValue("X-Source-IP", out var source) && !string.IsNullOrWhiteSpace(source))
        {
            return source.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? string.Empty;
        }

        if (headers.TryGetValue("X-Forwarded-For", out var forwarded) && !string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? string.Empty;
        }

        return string.Empty;
    }

    private static bool IsAllowedWebhookIp(string sourceIp, string configuredRanges)
    {
        if (!IPAddress.TryParse(sourceIp, out var ip))
        {
            return false;
        }

        var ranges = string.IsNullOrWhiteSpace(configuredRanges)
            ? DefaultWebhookCidrs
            : configuredRanges.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return ranges.Any(range => IsInCidr(ip, range));
    }

    private static bool IsInCidr(IPAddress address, string cidr)
    {
        var parts = cidr.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
        {
            return IPAddress.TryParse(parts[0], out var single) && single.Equals(address);
        }

        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var network) || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var prefixLength))
        {
            return false;
        }

        var addressBytes = address.GetAddressBytes();
        var networkBytes = network.GetAddressBytes();
        if (addressBytes.Length != networkBytes.Length)
        {
            return false;
        }

        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        for (var i = 0; i < fullBytes; i++)
        {
            if (addressBytes[i] != networkBytes[i])
            {
                return false;
            }
        }

        if (remainingBits == 0)
        {
            return true;
        }

        var mask = (byte)(0xFF << (8 - remainingBits));
        return (addressBytes[fullBytes] & mask) == (networkBytes[fullBytes] & mask);
    }
}

public sealed class RoboKassaPaymentProvider : IPaymentProvider, IPaymentWebhookVerifier, IPaymentStatusMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };
    private readonly PaymentProviderAccountService _accounts;
    private readonly IHostEnvironment _environment;

    public RoboKassaPaymentProvider(PaymentProviderAccountService accounts, IHostEnvironment environment)
    {
        _accounts = accounts;
        _environment = environment;
    }

    public PaymentProvider Provider => PaymentProvider.RoboKassa;

    public Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
    {
        EnsureEnabled(request.Account, Provider);
        if (IsLocalSandboxEnvironment() && request.Account.Mode == PaymentProviderMode.Sandbox && string.IsNullOrWhiteSpace(request.Account.SecretKeyProtected))
        {
            return Task.FromResult(RemainingPaymentProviderShared.LocalSandboxInit(request, "robokassa", Provider));
        }

        var password1 = _accounts.GetSecretKey(request.Account);
        if (string.IsNullOrWhiteSpace(request.Account.ShopId) || string.IsNullOrWhiteSpace(password1))
        {
            throw new InvalidOperationException("Robokassa MerchantLogin and Password#1 are required.");
        }

        var invId = CreateRobokassaInvoiceId(request.Payment.Id);
        var outSum = request.Order.Amount.ToString("0.00", CultureInfo.InvariantCulture);
        var shp = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["Shp_account"] = request.Account.ShopId,
            ["Shp_order"] = request.Order.Id.ToString("N"),
            ["Shp_payment"] = request.Payment.Id.ToString("N")
        };
        var signature = BuildRobokassaSignature(outSum, invId, password1, shp, GetHashAlgorithm(request.Account));
        var baseUrl = string.IsNullOrWhiteSpace(request.Account.ApiBaseUrl) ? "https://auth.robokassa.ru/Merchant/Index.aspx" : request.Account.ApiBaseUrl;
        var parameters = new Dictionary<string, string>
        {
            ["MerchantLogin"] = request.Account.ShopId,
            ["OutSum"] = outSum,
            ["InvId"] = invId,
            ["Description"] = $"VPN order {request.Order.Id}",
            ["SignatureValue"] = signature,
            ["Culture"] = "ru",
            ["Encoding"] = "utf-8"
        };
        foreach (var item in shp)
        {
            parameters[item.Key] = item.Value;
        }

        if (request.Account.Mode == PaymentProviderMode.Sandbox)
        {
            parameters["IsTest"] = "1";
        }

        var redirectUrl = AppendQuery(baseUrl, parameters);
        request.Payment.RawRequest = JsonSerializer.Serialize(new { provider = "RoboKassa", parameters = MaskRobokassaParams(parameters) }, JsonOptions);
        var rawResponse = JsonSerializer.Serialize(new { invId, redirectUrl, mode = request.Account.Mode.ToString() }, JsonOptions);
        return Task.FromResult(new PaymentInitResult(invId, redirectUrl, rawResponse));
    }

    public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var form = ParseForm(rawBody);
        var invId = GetFirst(form, "InvId", "InvID", "invoiceID");
        var outSum = GetFirst(form, "OutSum", "IncSum", "incSum");
        var status = GetFirst(form, "State", "state", "Shp_status");
        var amount = decimal.TryParse(outSum, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedAmount) ? parsedAmount : (decimal?)null;
        var eventId = $"robokassa.result:{invId}";
        var paid = string.IsNullOrWhiteSpace(status) || string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase);
        var providerAccount = GetFirst(form, "Shp_account");
        return Task.FromResult(new PaymentWebhookParseResult(eventId, "payment.succeeded", invId, RoboKassaPaymentStatusMapper.MapPaymentStatus(status, paid), rawBody, false, amount, "RUB", paid, providerAccount, GetFirst(form, "Shp_order")));
    }

    public Task<PaymentWebhookVerificationResult> VerifyAsync(PaymentProviderAccount account, PaymentWebhookParseResult parsed, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var password2 = _accounts.GetWebhookSecret(account);
        if (string.IsNullOrWhiteSpace(password2))
        {
            return Task.FromResult(new PaymentWebhookVerificationResult(false, "robokassa-password2", "Robokassa Password#2 is required for ResultURL verification."));
        }

        var form = ParseForm(rawBody);
        var signature = GetFirst(form, "SignatureValue");
        var invId = GetFirst(form, "InvId", "InvID", "invoiceID");
        var outSum = GetFirst(form, "OutSum", "IncSum", "incSum");
        var shp = ExtractShp(form);
        var expected = BuildRobokassaSignature(outSum, invId, password2, shp, GetHashAlgorithm(account));
        var valid = FixedEqualsHex(signature, expected);
        return Task.FromResult(valid
            ? new PaymentWebhookVerificationResult(true, "robokassa-signature", null)
            : new PaymentWebhookVerificationResult(false, "robokassa-signature", "Invalid Robokassa SignatureValue."));
    }

    public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
        => throw new NotSupportedException("Robokassa manual status recheck requires Partner API credentials and is fail-closed in this adapter. Use ResultURL webhook as source of truth or add Partner API settings.");

    public Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken)
        => throw new NotSupportedException("Robokassa refunds require Partner API credentials and are fail-closed in this adapter.");

    public PaymentStatus MapPaymentStatus(string providerStatus, bool paid) => RoboKassaPaymentStatusMapper.MapPaymentStatus(providerStatus, paid);
    public RefundStatus MapRefundStatus(string providerStatus) => RefundStatus.Unknown;

    public static string BuildRobokassaSignature(string outSum, string invId, string password, SortedDictionary<string, string> shp, string algorithm)
    {
        var baseString = $"{outSum}:{invId}:{password}";
        foreach (var item in shp.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            baseString += $":{item.Key}={item.Value}";
        }

        return ComputeHashHex(baseString, algorithm);
    }

    private static string CreateRobokassaInvoiceId(Guid paymentId)
    {
        var bytes = paymentId.ToByteArray();
        var value = BitConverter.ToUInt64(bytes, 0) % 900000000000UL + 100000000000UL;
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static SortedDictionary<string, string> ExtractShp(IReadOnlyDictionary<string, string> form)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in form)
        {
            if (pair.Key.StartsWith("Shp_", StringComparison.Ordinal))
            {
                result[pair.Key] = pair.Value;
            }
        }

        return result;
    }

    private static Dictionary<string, string> MaskRobokassaParams(Dictionary<string, string> parameters)
        => parameters.ToDictionary(x => x.Key, x => x.Key.Contains("Signature", StringComparison.OrdinalIgnoreCase) ? "***" : x.Value);

    private bool IsLocalSandboxEnvironment()
        => _environment.IsDevelopment()
            || _environment.IsEnvironment("Local")
            || _environment.IsEnvironment("Test")
            || _environment.IsEnvironment("Testing")
            || _environment.IsEnvironment("Sandbox");
}

public sealed class YooMoneyPaymentProvider : IPaymentProvider, IPaymentWebhookVerifier, IPaymentStatusMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };
    private readonly PaymentProviderAccountService _accounts;
    private readonly IHostEnvironment _environment;

    public YooMoneyPaymentProvider(PaymentProviderAccountService accounts, IHostEnvironment environment)
    {
        _accounts = accounts;
        _environment = environment;
    }
    public PaymentProvider Provider => PaymentProvider.YooMoney;

    public Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
    {
        EnsureEnabled(request.Account, Provider);
        if (string.IsNullOrWhiteSpace(request.Account.ShopId))
        {
            throw new InvalidOperationException("YooMoney receiver wallet is required in ShopId.");
        }

        var label = $"ym_{request.Payment.Id:N}";
        var parameters = new Dictionary<string, string>
        {
            ["receiver"] = request.Account.ShopId,
            ["quickpay-form"] = "button",
            ["targets"] = $"VPN order {request.Order.Id}",
            ["paymentType"] = "AC",
            ["sum"] = request.Order.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            ["label"] = label,
            ["successURL"] = request.ReturnUrl
        };
        var redirectUrl = AppendQuery(string.IsNullOrWhiteSpace(request.Account.ApiBaseUrl) ? "https://yoomoney.ru/quickpay/confirm" : request.Account.ApiBaseUrl, parameters);
        request.Payment.RawRequest = JsonSerializer.Serialize(new { provider = "YooMoney", parameters }, JsonOptions);
        var rawResponse = JsonSerializer.Serialize(new { label, redirectUrl, mode = request.Account.Mode.ToString() }, JsonOptions);
        return Task.FromResult(new PaymentInitResult(label, redirectUrl, rawResponse));
    }

    public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var form = ParseForm(rawBody);
        var label = GetFirst(form, "label");
        var operationId = GetFirst(form, "operation_id");
        var amountText = GetFirst(form, "amount");
        var currencyCode = GetFirst(form, "currency");
        var codepro = ParseBool(GetFirst(form, "codepro"));
        var unaccepted = ParseBool(GetFirst(form, "unaccepted"));
        var amount = decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedAmount) ? parsedAmount : (decimal?)null;
        var status = YooMoneyPaymentStatusMapper.MapPaymentStatus(codepro, unaccepted);
        var eventId = string.IsNullOrWhiteSpace(operationId) ? $"yoomoney:{label}:{ComputeSha256Hex(rawBody)[..16]}" : $"yoomoney:{operationId}";
        var receiver = GetFirst(form, "receiver");
        return Task.FromResult(new PaymentWebhookParseResult(eventId, "payment.incoming", label, status, rawBody, false, amount, NormalizeYooMoneyCurrency(currencyCode), status == PaymentStatus.Succeeded, receiver));
    }

    public Task<PaymentWebhookVerificationResult> VerifyAsync(PaymentProviderAccount account, PaymentWebhookParseResult parsed, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var secret = _accounts.GetWebhookSecret(account);
        if (string.IsNullOrWhiteSpace(secret))
        {
            secret = _accounts.GetSecretKey(account);
        }

        if (IsLocalSandboxEnvironment() && account.Mode == PaymentProviderMode.Sandbox && string.IsNullOrWhiteSpace(secret))
        {
            if (headers.TryGetValue("X-YooMoney-Sandbox-Webhook", out var marker) && string.Equals(marker, "true", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new PaymentWebhookVerificationResult(true, "local-sandbox-header", null));
            }

            return Task.FromResult(new PaymentWebhookVerificationResult(false, "local-sandbox-header", "Missing X-YooMoney-Sandbox-Webhook=true header."));
        }

        if (account.Mode == PaymentProviderMode.Production && headers.TryGetValue("X-YooMoney-Sandbox-Webhook", out var sandboxMarker) && string.Equals(sandboxMarker, "true", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new PaymentWebhookVerificationResult(false, "production-sandbox-header-denied", "Local sandbox webhook header is forbidden for production provider accounts."));
        }

        var form = ParseForm(rawBody);
        var providedSign = GetFirst(form, "sign");
        if (string.IsNullOrWhiteSpace(providedSign))
        {
            return Task.FromResult(new PaymentWebhookVerificationResult(false, "yoomoney-hmac-sha256", "Missing YooMoney sign parameter."));
        }

        var expected = BuildYooMoneySign(form, secret);
        var valid = FixedEqualsHex(providedSign, expected);
        return Task.FromResult(valid
            ? new PaymentWebhookVerificationResult(true, "yoomoney-hmac-sha256", null)
            : new PaymentWebhookVerificationResult(false, "yoomoney-hmac-sha256", "Invalid YooMoney sign."));
    }

    public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
        => throw new NotSupportedException("YooMoney quickpay manual recheck requires Wallet API OAuth token and is fail-closed in this adapter.");

    public Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken)
        => throw new NotSupportedException("YooMoney quickpay refunds are not supported by this collection-form adapter.");

    public PaymentStatus MapPaymentStatus(string providerStatus, bool paid) => paid ? PaymentStatus.Succeeded : PaymentStatus.Unknown;
    public RefundStatus MapRefundStatus(string providerStatus) => RefundStatus.Unknown;

    private bool IsLocalSandboxEnvironment()
        => _environment.IsDevelopment()
            || _environment.IsEnvironment("Local")
            || _environment.IsEnvironment("Test")
            || _environment.IsEnvironment("Testing")
            || _environment.IsEnvironment("Sandbox");

    private static string NormalizeYooMoneyCurrency(string value)
        => string.Equals(value, "643", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "RUB", StringComparison.OrdinalIgnoreCase)
            ? "RUB"
            : string.IsNullOrWhiteSpace(value)
                ? "INVALID"
                : value.Trim().ToUpperInvariant();

    public static string BuildYooMoneySign(IReadOnlyDictionary<string, string> form, string secret)
    {
        var canonical = string.Join("&", form
            .Where(x => !string.Equals(x.Key, "sign", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => $"{Rfc3986Encode(x.Key)}={Rfc3986Encode(x.Value)}"));
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public class PaymentProviderFactory : IPaymentProviderFactory
{
    private readonly IReadOnlyDictionary<PaymentProvider, IPaymentProvider> _providers;

    public PaymentProviderFactory(IEnumerable<IPaymentProvider> providers)
    {
        _providers = providers.ToDictionary(x => x.Provider, x => x);
    }

    public IPaymentProvider Get(PaymentProvider provider)
        => _providers.TryGetValue(provider, out var paymentProvider)
            ? paymentProvider
            : throw new InvalidOperationException($"Payment provider '{provider}' is not registered.");
}

internal static class PaymentProviderShared
{
    public static void EnsureEnabled(PaymentProviderAccount account, PaymentProvider provider)
    {
        if (account.Provider != provider)
        {
            throw new InvalidOperationException($"Payment provider account {account.Id} belongs to {account.Provider}, not {provider}.");
        }

        if (account.Mode == PaymentProviderMode.Disabled || !account.IsEnabled)
        {
            throw new InvalidOperationException($"{provider} account is disabled.");
        }
    }

    public static string AppendQuery(string baseUrl, IReadOnlyDictionary<string, string> parameters)
    {
        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return baseUrl + separator + string.Join("&", parameters.Select(x => $"{WebUtility.UrlEncode(x.Key)}={WebUtility.UrlEncode(x.Value)}"));
    }

    public static Dictionary<string, string> ParseForm(string rawBody)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return result;
        }

        foreach (var pair in rawBody.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');
            var key = idx >= 0 ? pair[..idx] : pair;
            var value = idx >= 0 ? pair[(idx + 1)..] : string.Empty;
            result[WebUtility.UrlDecode(key)] = WebUtility.UrlDecode(value);
        }

        return result;
    }

    public static string GetFirst(IReadOnlyDictionary<string, string> values, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    public static bool ParseBool(string value)
        => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";

    public static string NormalizeCurrency(string value)
        => value == "643" ? "RUB" : string.IsNullOrWhiteSpace(value) ? "RUB" : value.Trim().ToUpperInvariant();

    public static string GetHashAlgorithm(PaymentProviderAccount account)
    {
        if (string.IsNullOrWhiteSpace(account.ExtraSettingsJson))
        {
            return "MD5";
        }

        try
        {
            using var doc = JsonDocument.Parse(account.ExtraSettingsJson);
            return doc.RootElement.TryGetProperty("hashAlgorithm", out var value) ? value.GetString() ?? "MD5" : "MD5";
        }
        catch
        {
            return "MD5";
        }
    }

    public static string ComputeHashHex(string input, string algorithm)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = algorithm.Trim().ToUpperInvariant() switch
        {
            "SHA256" => SHA256.HashData(bytes),
            "SHA512" => SHA512.HashData(bytes),
            _ => MD5.HashData(bytes)
        };
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ComputeSha256Hex(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input ?? string.Empty));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool FixedEqualsHex(string provided, string expected)
    {
        if (string.IsNullOrWhiteSpace(provided) || string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        var normalizedProvided = provided.Trim().ToLowerInvariant();
        var normalizedExpected = expected.Trim().ToLowerInvariant();
        var left = Encoding.UTF8.GetBytes(normalizedProvided);
        var right = Encoding.UTF8.GetBytes(normalizedExpected);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }

    public static string Rfc3986Encode(string value)
        => Uri.EscapeDataString(value ?? string.Empty).Replace("%7E", "~", StringComparison.Ordinal);
}
