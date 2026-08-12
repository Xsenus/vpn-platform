using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Hosting;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Security;
using static VpnPlatform.Infrastructure.Payments.PaymentProviderShared;

namespace VpnPlatform.Infrastructure.Payments;

internal static class RemainingPaymentProviderShared
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public static bool IsLocalSandbox(IHostEnvironment environment, PaymentProviderAccount account)
        => PaymentProviderConfigurationRules.IsCredentiallessLocalSandbox(account, environment.EnvironmentName);

    public static PaymentWebhookVerificationResult VerifyLocalSandboxHeader(IHostEnvironment environment, PaymentProviderAccount account, IReadOnlyDictionary<string, string> headers, string headerName)
    {
        if (environment.IsProduction() && headers.TryGetValue(headerName, out var productionMarker) && string.Equals(productionMarker, "true", StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentWebhookVerificationResult(false, "production-sandbox-header-denied", $"{headerName}=true is forbidden in production.");
        }

        if (IsLocalSandbox(environment, account))
        {
            return headers.TryGetValue(headerName, out var marker) && string.Equals(marker, "true", StringComparison.OrdinalIgnoreCase)
                ? new PaymentWebhookVerificationResult(true, "local-sandbox-header", null)
                : new PaymentWebhookVerificationResult(false, "local-sandbox-header", $"Missing {headerName}=true header.");
        }

        return new PaymentWebhookVerificationResult(true, "provider-signature", null);
    }

    public static long ToMinorUnits(decimal amount, string currency)
    {
        var decimals = currency.Equals("JPY", StringComparison.OrdinalIgnoreCase) || currency.Equals("XTR", StringComparison.OrdinalIgnoreCase) ? 0 : 2;
        var multiplier = decimals == 0 ? 1m : 100m;
        return (long)decimal.Round(amount * multiplier, 0, MidpointRounding.AwayFromZero);
    }

    public static decimal FromMinorUnits(long amountMinor, string currency)
    {
        var decimals = currency.Equals("JPY", StringComparison.OrdinalIgnoreCase) || currency.Equals("XTR", StringComparison.OrdinalIgnoreCase) ? 0 : 2;
        var divisor = decimals == 0 ? 1m : 100m;
        return decimal.Round(amountMinor / divisor, decimals, MidpointRounding.AwayFromZero);
    }

    public static string GetExtra(PaymentProviderAccount account, string key, string fallback = "")
    {
        if (string.IsNullOrWhiteSpace(account.ExtraSettingsJson))
        {
            return fallback;
        }

        try
        {
            using var document = JsonDocument.Parse(account.ExtraSettingsJson);
            return document.RootElement.TryGetProperty(key, out var value) ? value.ToString() : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    public static string SafePaymentDescription(Order order, int maxLength = 128)
    {
        var text = $"VPN order {order.Id}";
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    public static string SerializeRaw(object value) => JsonSerializer.Serialize(value, JsonOptions);

    public static string SandboxPaymentId(string prefix, Guid paymentId) => $"{prefix}_sandbox_{paymentId:N}";

    public static string NormalizeProviderCurrency(string value)
        => string.IsNullOrWhiteSpace(value)
            ? "INVALID"
            : value.Trim().Equals("643", StringComparison.OrdinalIgnoreCase)
                ? "RUB"
                : value.Trim().ToUpperInvariant();

    public static PaymentInitResult LocalSandboxInit(PaymentCreateRequest request, string prefix, PaymentProvider provider)
    {
        var providerPaymentId = SandboxPaymentId(prefix, request.Payment.Id);
        var redirectUrl = AppendQuery(request.ReturnUrl, new Dictionary<string, string>
        {
            ["paymentId"] = providerPaymentId,
            ["provider"] = provider.ToString(),
            ["sandbox"] = "1"
        });
        request.Payment.RawRequest = SerializeRaw(new
        {
            provider = provider.ToString(),
            mode = "Sandbox",
            orderId = request.Order.Id,
            paymentAttemptId = request.Payment.Id,
            amount = request.Order.Amount,
            currency = request.Order.Currency,
            returnUrl = request.ReturnUrl
        });
        var raw = SerializeRaw(new
        {
            id = providerPaymentId,
            status = "pending",
            amount = request.Order.Amount,
            currency = request.Order.Currency,
            redirectUrl
        });
        return new PaymentInitResult(providerPaymentId, redirectUrl, raw);
    }

    public static PaymentRefundResult LocalSandboxRefund(PaymentAttempt payment, decimal amount, string reason, string prefix)
    {
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{payment.Id:N}:{amount.ToString("0.00", CultureInfo.InvariantCulture)}:{reason}"))).ToLowerInvariant();
        var refundId = $"{prefix}_refund_sandbox_{fingerprint[..24]}";
        var raw = SerializeRaw(new
        {
            id = refundId,
            status = "succeeded",
            amount,
            currency = payment.Currency,
            reason
        });
        return new PaymentRefundResult(refundId, RefundStatus.Succeeded, raw, payment.ProviderPaymentId, amount, payment.Currency, payment.Id.ToString("N"));
    }

    public static bool FixedEqualsBase64(string provided, byte[] expectedBytes)
    {
        if (string.IsNullOrWhiteSpace(provided))
        {
            return false;
        }

        var expected = Convert.ToBase64String(expectedBytes);
        var left = Encoding.UTF8.GetBytes(provided.Trim());
        var right = Encoding.UTF8.GetBytes(expected);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }

    public static Dictionary<string, string> ParseJsonOrForm(string rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var trimmed = rawBody.Trim();
        if (!trimmed.StartsWith('{'))
        {
            return ParseForm(rawBody);
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var document = JsonDocument.Parse(trimmed);
        FlattenJson(document.RootElement, string.Empty, result);
        return result;
    }

    private static void FlattenJson(JsonElement element, string prefix, IDictionary<string, string> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrWhiteSpace(prefix) ? property.Name : $"{prefix}.{property.Name}";
                    FlattenJson(property.Value, key, result);
                }
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    FlattenJson(item, $"{prefix}[{index++}]", result);
                }
                break;
            case JsonValueKind.String:
                result[prefix] = element.GetString() ?? string.Empty;
                break;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                result[prefix] = element.ToString();
                break;
        }
    }
}

public sealed class StripePaymentProvider : IPaymentProvider, IPaymentWebhookVerifier, IPaymentStatusMapper, IPaymentRefundStatusProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PaymentProviderAccountService _accounts;
    private readonly IHostEnvironment _environment;

    public StripePaymentProvider(IHttpClientFactory httpClientFactory, PaymentProviderAccountService accounts, IHostEnvironment environment)
    {
        _httpClientFactory = httpClientFactory;
        _accounts = accounts;
        _environment = environment;
    }

    public PaymentProvider Provider => PaymentProvider.Stripe;

    public async Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
    {
        EnsureEnabled(request.Account, Provider);
        if (RemainingPaymentProviderShared.IsLocalSandbox(_environment, request.Account))
        {
            return RemainingPaymentProviderShared.LocalSandboxInit(request, "stripe", Provider);
        }

        var secret = _accounts.GetSecretKey(request.Account);
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("Stripe secret key is required.");
        }

        var amountMinor = RemainingPaymentProviderShared.ToMinorUnits(request.Order.Amount, request.Order.Currency);
        var form = new Dictionary<string, string>
        {
            ["mode"] = "payment",
            ["success_url"] = AppendQuery(request.ReturnUrl, new Dictionary<string, string> { ["provider"] = "Stripe", ["paymentId"] = "{CHECKOUT_SESSION_ID}", ["status"] = "success" }),
            ["cancel_url"] = AppendQuery(request.ReturnUrl, new Dictionary<string, string> { ["provider"] = "Stripe", ["status"] = "cancel" }),
            ["line_items[0][quantity]"] = "1",
            ["line_items[0][price_data][currency]"] = request.Order.Currency.ToLowerInvariant(),
            ["line_items[0][price_data][unit_amount]"] = amountMinor.ToString(CultureInfo.InvariantCulture),
            ["line_items[0][price_data][product_data][name]"] = RemainingPaymentProviderShared.SafePaymentDescription(request.Order, 80),
            ["metadata[orderId]"] = request.Order.Id.ToString("N"),
            ["metadata[paymentAttemptId]"] = request.Payment.Id.ToString("N"),
            ["metadata[providerAccountId]"] = request.Account.Id.ToString("N")
        };

        request.Payment.RawRequest = RemainingPaymentProviderShared.SerializeRaw(new { provider = "Stripe", form = form.ToDictionary(x => x.Key, x => x.Value) });
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildStripeUri(request.Account, "/v1/checkout/sessions"));
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
        httpRequest.Headers.Add("Idempotency-Key", request.Payment.IdempotencyKey);
        httpRequest.Content = new FormUrlEncodedContent(form);

        using var response = await SendStripeAsync(httpRequest, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Stripe checkout session creation failed with HTTP {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(raw);
        var root = document.RootElement;
        var id = root.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? string.Empty : string.Empty;
        var url = root.TryGetProperty("url", out var urlElement) ? urlElement.GetString() ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("Stripe response does not contain Checkout Session id/url.");
        }

        return new PaymentInitResult(id, url, raw);
    }

    public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(rawBody) ? "{}" : rawBody);
        var root = document.RootElement;
        var eventId = root.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? string.Empty : string.Empty;
        var eventType = root.TryGetProperty("type", out var typeElement) ? typeElement.GetString() ?? string.Empty : string.Empty;
        var obj = root.TryGetProperty("data", out var data) && data.TryGetProperty("object", out var objectElement) ? objectElement : root;
        var sessionId = obj.TryGetProperty("id", out var sessionIdElement) ? sessionIdElement.GetString() ?? string.Empty : string.Empty;
        var paymentStatus = obj.TryGetProperty("payment_status", out var paymentStatusElement) ? paymentStatusElement.GetString() ?? string.Empty : string.Empty;
        var status = MapPaymentStatus(paymentStatus, string.Equals(paymentStatus, "paid", StringComparison.OrdinalIgnoreCase));
        if (eventType is "checkout.session.expired") status = PaymentStatus.Cancelled;
        var amountMinor = obj.TryGetProperty("amount_total", out var amountElement) && amountElement.TryGetInt64(out var parsedMinor) ? parsedMinor : 0L;
        var currency = obj.TryGetProperty("currency", out var currencyElement) ? currencyElement.GetString()?.ToUpperInvariant() : null;
        decimal? amount = amountMinor > 0 && !string.IsNullOrWhiteSpace(currency) ? RemainingPaymentProviderShared.FromMinorUnits(amountMinor, currency) : null;
        if (status == PaymentStatus.Succeeded && (!amount.HasValue || string.IsNullOrWhiteSpace(currency)))
        {
            status = PaymentStatus.Unknown;
        }
        var orderId = TryGetMetadata(obj, "orderId");
        return Task.FromResult(new PaymentWebhookParseResult(eventId, eventType, sessionId, status, rawBody, false, amount, currency, status == PaymentStatus.Succeeded, null, orderId));
    }

    public Task<PaymentWebhookVerificationResult> VerifyAsync(PaymentProviderAccount account, PaymentWebhookParseResult parsed, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var local = RemainingPaymentProviderShared.VerifyLocalSandboxHeader(_environment, account, headers, "X-Stripe-Sandbox-Webhook");
        if (local.Method == "local-sandbox-header" || !local.IsValid)
        {
            return Task.FromResult(local);
        }

        var secret = _accounts.GetWebhookSecret(account);
        if (string.IsNullOrWhiteSpace(secret))
        {
            return Task.FromResult(new PaymentWebhookVerificationResult(false, "stripe-signature", "Stripe webhook endpoint secret is required."));
        }

        if (!headers.TryGetValue("Stripe-Signature", out var signatureHeader) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return Task.FromResult(new PaymentWebhookVerificationResult(false, "stripe-signature", "Missing Stripe-Signature header."));
        }

        var parts = signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.Split('=', 2))
            .Where(x => x.Length == 2)
            .GroupBy(x => x[0])
            .ToDictionary(x => x.Key, x => x.Select(v => v[1]).ToList(), StringComparer.OrdinalIgnoreCase);
        if (!parts.TryGetValue("t", out var timestamps) || !long.TryParse(timestamps.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ts)
            || !parts.TryGetValue("v1", out var signatures))
        {
            return Task.FromResult(new PaymentWebhookVerificationResult(false, "stripe-signature", "Stripe-Signature header is malformed."));
        }

        var age = Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts);
        if (age > 300)
        {
            return Task.FromResult(new PaymentWebhookVerificationResult(false, "stripe-signature", "Stripe webhook timestamp is outside tolerance."));
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{ts}.{rawBody}"))).ToLowerInvariant();
        var valid = signatures.Any(sig => FixedEqualsHex(sig, expected));
        return Task.FromResult(valid
            ? new PaymentWebhookVerificationResult(true, "stripe-signature", null)
            : new PaymentWebhookVerificationResult(false, "stripe-signature", "Invalid Stripe webhook signature."));
    }

    public async Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
    {
        if (RemainingPaymentProviderShared.IsLocalSandbox(_environment, account))
        {
            return new PaymentStatusResult(payment.ProviderPaymentId, payment.Status, payment.RawResponse, payment.StatusReason);
        }

        var secret = _accounts.GetSecretKey(account);
        if (string.IsNullOrWhiteSpace(secret))
        {
            return new PaymentStatusResult(payment.ProviderPaymentId, PaymentStatus.Unknown, "{}", "stripe_secret_key_missing");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildStripeUri(account, $"/v1/checkout/sessions/{WebUtility.UrlEncode(payment.ProviderPaymentId)}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
        using var response = await SendStripeAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new PaymentStatusResult(payment.ProviderPaymentId, PaymentStatus.Unknown, raw, $"http_{(int)response.StatusCode}");
        }

        using var document = JsonDocument.Parse(raw);
        var root = document.RootElement;
        var responsePaymentId = root.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty;
        var paymentStatus = root.TryGetProperty("payment_status", out var ps) ? ps.GetString() ?? string.Empty : string.Empty;
        var paid = string.Equals(paymentStatus, "paid", StringComparison.OrdinalIgnoreCase)
            || string.Equals(paymentStatus, "no_payment_required", StringComparison.OrdinalIgnoreCase);
        var currency = root.TryGetProperty("currency", out var currencyElement) ? currencyElement.GetString()?.ToUpperInvariant() : null;
        var amount = root.TryGetProperty("amount_total", out var amountElement) && amountElement.TryGetInt64(out var amountMinor)
            && !string.IsNullOrWhiteSpace(currency)
                ? RemainingPaymentProviderShared.FromMinorUnits(amountMinor, currency)
                : (decimal?)null;
        var status = MapPaymentStatus(paymentStatus, paid);
        var orderId = TryGetMetadata(root, "orderId");
        var statusReason = status == PaymentStatus.Succeeded
            && (string.IsNullOrWhiteSpace(responsePaymentId) || !amount.HasValue || string.IsNullOrWhiteSpace(currency) || string.IsNullOrWhiteSpace(orderId))
                ? "stripe_payment_proof_incomplete"
                : paymentStatus;
        if (statusReason == "stripe_payment_proof_incomplete")
        {
            status = PaymentStatus.Unknown;
        }
        return new PaymentStatusResult(
            responsePaymentId,
            status,
            raw,
            statusReason,
            amount,
            currency,
            orderId,
            Paid: paid);
    }

    public async Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken)
    {
        if (RemainingPaymentProviderShared.IsLocalSandbox(_environment, account))
        {
            return RemainingPaymentProviderShared.LocalSandboxRefund(payment, amount, reason, "stripe");
        }

        var secret = _accounts.GetSecretKey(account);
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("Stripe secret key is required.");
        }

        var paymentIntent = await ResolveStripePaymentIntentAsync(payment, account, secret, cancellationToken);
        if (string.IsNullOrWhiteSpace(paymentIntent))
        {
            throw new NotSupportedException("Stripe refund requires a paid Checkout Session with payment_intent available.");
        }

        var form = new Dictionary<string, string>
        {
            ["payment_intent"] = paymentIntent,
            ["amount"] = RemainingPaymentProviderShared.ToMinorUnits(amount, payment.Currency).ToString(CultureInfo.InvariantCulture),
            ["metadata[paymentAttemptId]"] = payment.Id.ToString("N")
        };
        if (!string.IsNullOrWhiteSpace(reason)) form["metadata[reason]"] = reason;
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildStripeUri(account, "/v1/refunds"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
        request.Headers.Add("Idempotency-Key", $"refund-{payment.Id:N}-{RemainingPaymentProviderShared.ToMinorUnits(amount, payment.Currency)}-{ComputeSha256Hex(reason)[..16]}");
        request.Content = new FormUrlEncodedContent(form);
        using var response = await SendStripeAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Stripe refund failed with HTTP {(int)response.StatusCode}.");
        }

        return ParseRefundResult(raw, paymentIntent);
    }

    public async Task<PaymentRefundResult> GetRefundStatusAsync(
        PaymentAttempt payment,
        PaymentProviderAccount account,
        Refund refund,
        CancellationToken cancellationToken)
    {
        if (RemainingPaymentProviderShared.IsLocalSandbox(_environment, account))
        {
            return new PaymentRefundResult(refund.ProviderRefundId, refund.Status, refund.RawResponse, Amount: refund.Amount, Currency: refund.Currency);
        }

        var secret = _accounts.GetSecretKey(account);
        if (string.IsNullOrWhiteSpace(secret))
        {
            return new PaymentRefundResult(refund.ProviderRefundId, RefundStatus.Unknown, "{}", StatusReason: "stripe_secret_key_missing");
        }

        var paymentIntent = await ResolveStripePaymentIntentAsync(payment, account, secret, cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildStripeUri(account, $"/v1/refunds/{WebUtility.UrlEncode(refund.ProviderRefundId)}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
        using var response = await SendStripeAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new PaymentRefundResult(refund.ProviderRefundId, RefundStatus.Unknown, raw, StatusReason: $"http_{(int)response.StatusCode}");
        }

        return ParseRefundResult(raw, paymentIntent);
    }

    public PaymentStatus MapPaymentStatus(string providerStatus, bool paid)
        => providerStatus.Trim().ToLowerInvariant() switch
        {
            "paid" => PaymentStatus.Succeeded,
            "unpaid" => PaymentStatus.Pending,
            "no_payment_required" => PaymentStatus.Succeeded,
            _ => PaymentStatus.Unknown
        };

    public RefundStatus MapRefundStatus(string providerStatus)
        => providerStatus.Trim().ToLowerInvariant() switch
        {
            "succeeded" => RefundStatus.Succeeded,
            "pending" => RefundStatus.Pending,
            "failed" => RefundStatus.Failed,
            "canceled" => RefundStatus.Cancelled,
            _ => RefundStatus.Unknown
        };

    private PaymentRefundResult ParseRefundResult(string raw, string expectedPaymentIntent)
    {
        using var document = JsonDocument.Parse(raw);
        var root = document.RootElement;
        var refundId = root.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty;
        var status = root.TryGetProperty("status", out var statusElement) ? MapRefundStatus(statusElement.GetString() ?? string.Empty) : RefundStatus.Unknown;
        var paymentIntentId = root.TryGetProperty("payment_intent", out var paymentIntentElement) ? paymentIntentElement.GetString() : null;
        var currency = root.TryGetProperty("currency", out var currencyElement) ? currencyElement.GetString()?.ToUpperInvariant() : null;
        var refundAmount = root.TryGetProperty("amount", out var amountElement) && amountElement.TryGetInt64(out var amountMinor)
            && !string.IsNullOrWhiteSpace(currency)
                ? RemainingPaymentProviderShared.FromMinorUnits(amountMinor, currency)
                : (decimal?)null;
        var paymentAttemptId = TryGetMetadata(root, "paymentAttemptId");
        var proofStatusReason = status != RefundStatus.Unknown
            && (string.IsNullOrWhiteSpace(refundId) || string.IsNullOrWhiteSpace(paymentIntentId) || !refundAmount.HasValue
                || string.IsNullOrWhiteSpace(currency) || string.IsNullOrWhiteSpace(paymentAttemptId))
                ? "stripe_refund_proof_incomplete"
                : null;
        if (proofStatusReason is not null)
        {
            status = RefundStatus.Unknown;
        }

        return new PaymentRefundResult(
            refundId,
            status,
            raw,
            ProviderPaymentId: paymentIntentId,
            Amount: refundAmount,
            Currency: currency,
            InternalPaymentAttemptId: paymentAttemptId,
            StatusReason: proofStatusReason,
            ExpectedProviderPaymentId: expectedPaymentIntent);
    }

    private async Task<string> ResolveStripePaymentIntentAsync(PaymentAttempt payment, PaymentProviderAccount account, string secret, CancellationToken cancellationToken)
    {
        static string Extract(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || !raw.TrimStart().StartsWith('{')) return string.Empty;
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return string.Empty;
                if (root.TryGetProperty("payment_intent", out var pi) && pi.ValueKind == JsonValueKind.String) return pi.GetString() ?? string.Empty;
                var obj = root.TryGetProperty("data", out var data)
                    && data.ValueKind == JsonValueKind.Object
                    && data.TryGetProperty("object", out var objectElement)
                    && objectElement.ValueKind == JsonValueKind.Object
                        ? objectElement
                        : root;
                return obj.TryGetProperty("payment_intent", out var pi2) && pi2.ValueKind == JsonValueKind.String
                    ? pi2.GetString() ?? string.Empty
                    : string.Empty;
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }

        var fromStored = Extract(payment.WebhookPayload) is { Length: > 0 } stored ? stored : Extract(payment.RawResponse);
        if (!string.IsNullOrWhiteSpace(fromStored)) return fromStored;
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildStripeUri(account, $"/v1/checkout/sessions/{WebUtility.UrlEncode(payment.ProviderPaymentId)}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
        using var response = await SendStripeAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        return response.IsSuccessStatusCode ? Extract(raw) : string.Empty;
    }

    private Task<HttpResponseMessage> SendStripeAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => _httpClientFactory.CreateClient("Stripe").SendAsync(request, cancellationToken);

    private static Uri BuildStripeUri(PaymentProviderAccount account, string path)
    {
        var baseUrl = string.IsNullOrWhiteSpace(account.ApiBaseUrl) ? "https://api.stripe.com" : account.ApiBaseUrl.TrimEnd('/');
        return new Uri($"{baseUrl}/{path.TrimStart('/')}", UriKind.Absolute);
    }

    private static string TryGetMetadata(JsonElement obj, string key)
    {
        if (obj.TryGetProperty("metadata", out var metadata) && metadata.ValueKind == JsonValueKind.Object && metadata.TryGetProperty(key, out var value))
        {
            return value.GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}

public sealed class PayPalPaymentProvider : IPaymentProvider, IPaymentWebhookVerifier, IPaymentStatusMapper, IPaymentApprovedOrderCaptureProvider, IPaymentRefundStatusProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PaymentProviderAccountService _accounts;
    private readonly IHostEnvironment _environment;

    public PayPalPaymentProvider(IHttpClientFactory httpClientFactory, PaymentProviderAccountService accounts, IHostEnvironment environment)
    {
        _httpClientFactory = httpClientFactory;
        _accounts = accounts;
        _environment = environment;
    }

    public PaymentProvider Provider => PaymentProvider.PayPal;

    public async Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
    {
        EnsureEnabled(request.Account, Provider);
        if (RemainingPaymentProviderShared.IsLocalSandbox(_environment, request.Account))
        {
            return RemainingPaymentProviderShared.LocalSandboxInit(request, "paypal", Provider);
        }

        var token = await GetAccessTokenAsync(request.Account, cancellationToken);
        var body = new JsonObject
        {
            ["intent"] = "CAPTURE",
            ["purchase_units"] = new JsonArray
            {
                new JsonObject
                {
                    ["reference_id"] = request.Payment.Id.ToString("N"),
                    ["invoice_id"] = request.Payment.Id.ToString("N"),
                    ["custom_id"] = request.Order.Id.ToString("N"),
                    ["description"] = RemainingPaymentProviderShared.SafePaymentDescription(request.Order, 120),
                    ["amount"] = new JsonObject
                    {
                        ["currency_code"] = request.Order.Currency.ToUpperInvariant(),
                        ["value"] = request.Order.Amount.ToString("0.00", CultureInfo.InvariantCulture)
                    }
                }
            },
            ["application_context"] = new JsonObject
            {
                ["return_url"] = AppendQuery(request.ReturnUrl, new Dictionary<string, string> { ["provider"] = "PayPal", ["status"] = "success" }),
                ["cancel_url"] = AppendQuery(request.ReturnUrl, new Dictionary<string, string> { ["provider"] = "PayPal", ["status"] = "cancel" })
            }
        };
        var rawRequest = body.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        request.Payment.RawRequest = rawRequest;
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildPayPalUri(request.Account, "/v2/checkout/orders"));
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpRequest.Headers.Add("PayPal-Request-Id", request.Payment.IdempotencyKey);
        httpRequest.Content = new StringContent(rawRequest, Encoding.UTF8, "application/json");
        using var response = await SendPayPalAsync(httpRequest, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"PayPal order creation failed with HTTP {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(raw);
        var root = document.RootElement;
        var id = root.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? string.Empty : string.Empty;
        var approve = root.TryGetProperty("links", out var links)
            ? links.EnumerateArray().FirstOrDefault(x => x.TryGetProperty("rel", out var rel) && string.Equals(rel.GetString(), "approve", StringComparison.OrdinalIgnoreCase))
            : default;
        var url = approve.ValueKind != JsonValueKind.Undefined && approve.TryGetProperty("href", out var href) ? href.GetString() ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("PayPal response does not contain order id/approve link.");
        }

        return new PaymentInitResult(id, url, raw);
    }

    public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(rawBody) ? "{}" : rawBody);
        var root = document.RootElement;
        var eventId = root.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? string.Empty : string.Empty;
        var eventType = root.TryGetProperty("event_type", out var eventElement) ? eventElement.GetString() ?? string.Empty : string.Empty;
        var resource = root.TryGetProperty("resource", out var resourceElement) ? resourceElement : root;
        var providerPaymentId = ResolvePayPalOrderId(resource);
        var statusText = resource.TryGetProperty("status", out var statusElement) ? statusElement.GetString() ?? string.Empty : eventType;
        var status = MapPaymentStatus(statusText, eventType.Equals("PAYMENT.CAPTURE.COMPLETED", StringComparison.OrdinalIgnoreCase));
        var amountElement = resource.TryGetProperty("amount", out var amountObj) ? amountObj : default;
        var currency = amountElement.ValueKind != JsonValueKind.Undefined && amountElement.TryGetProperty("currency_code", out var cur) ? cur.GetString() : null;
        var amount = amountElement.ValueKind != JsonValueKind.Undefined && amountElement.TryGetProperty("value", out var val) && decimal.TryParse(val.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedAmount) ? parsedAmount : (decimal?)null;
        if (status == PaymentStatus.Succeeded && (!amount.HasValue || string.IsNullOrWhiteSpace(currency)))
        {
            status = PaymentStatus.Unknown;
        }
        var orderId = resource.TryGetProperty("custom_id", out var custom) ? custom.GetString() ?? string.Empty : string.Empty;
        return Task.FromResult(new PaymentWebhookParseResult(eventId, eventType, providerPaymentId, status, rawBody, false, amount, currency, status == PaymentStatus.Succeeded, null, orderId));
    }

    public async Task<PaymentWebhookVerificationResult> VerifyAsync(PaymentProviderAccount account, PaymentWebhookParseResult parsed, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var local = RemainingPaymentProviderShared.VerifyLocalSandboxHeader(_environment, account, headers, "X-PayPal-Sandbox-Webhook");
        if (local.Method == "local-sandbox-header" || !local.IsValid)
        {
            return local;
        }

        var webhookId = _accounts.GetWebhookSecret(account);
        if (string.IsNullOrWhiteSpace(webhookId))
        {
            return new PaymentWebhookVerificationResult(false, "paypal-verify-webhook-signature", "PayPal Webhook ID is required in WebhookSecret.");
        }

        var requiredHeaders = new[] { "PAYPAL-AUTH-ALGO", "PAYPAL-CERT-URL", "PAYPAL-TRANSMISSION-ID", "PAYPAL-TRANSMISSION-SIG", "PAYPAL-TRANSMISSION-TIME" };
        if (requiredHeaders.Any(header => !headers.ContainsKey(header)))
        {
            return new PaymentWebhookVerificationResult(false, "paypal-verify-webhook-signature", "Missing PayPal webhook verification headers.");
        }

        var token = await GetAccessTokenAsync(account, cancellationToken);
        var body = new JsonObject
        {
            ["auth_algo"] = headers["PAYPAL-AUTH-ALGO"],
            ["cert_url"] = headers["PAYPAL-CERT-URL"],
            ["transmission_id"] = headers["PAYPAL-TRANSMISSION-ID"],
            ["transmission_sig"] = headers["PAYPAL-TRANSMISSION-SIG"],
            ["transmission_time"] = headers["PAYPAL-TRANSMISSION-TIME"],
            ["webhook_id"] = webhookId,
            ["webhook_event"] = JsonNode.Parse(rawBody)
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildPayPalUri(account, "/v1/notifications/verify-webhook-signature"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(body.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8, "application/json");
        using var response = await SendPayPalAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new PaymentWebhookVerificationResult(false, "paypal-verify-webhook-signature", $"PayPal verification failed with HTTP {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(raw);
        var status = document.RootElement.TryGetProperty("verification_status", out var statusElement) ? statusElement.GetString() ?? string.Empty : string.Empty;
        return string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase)
            ? new PaymentWebhookVerificationResult(true, "paypal-verify-webhook-signature", null)
            : new PaymentWebhookVerificationResult(false, "paypal-verify-webhook-signature", $"PayPal verification status is {status}.");
    }

    public async Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
    {
        if (RemainingPaymentProviderShared.IsLocalSandbox(_environment, account))
        {
            return new PaymentStatusResult(payment.ProviderPaymentId, payment.Status, payment.RawResponse, payment.StatusReason);
        }

        var token = await GetAccessTokenAsync(account, cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildPayPalUri(account, $"/v2/checkout/orders/{WebUtility.UrlEncode(payment.ProviderPaymentId)}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await SendPayPalAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new PaymentStatusResult(payment.ProviderPaymentId, PaymentStatus.Unknown, raw, $"http_{(int)response.StatusCode}");
        }

        var parsed = ParseOrderStatus(payment, raw);
        return parsed.IsSuccess && parsed.Value is not null
            ? parsed.Value
            : new PaymentStatusResult(payment.ProviderPaymentId, PaymentStatus.Unknown, raw, parsed.Error);
    }

    public bool RequiresCapture(PaymentWebhookParseResult webhook)
        => string.Equals(webhook.EventType, "CHECKOUT.ORDER.APPROVED", StringComparison.OrdinalIgnoreCase)
            && webhook.Status == PaymentStatus.WaitingConfirmation;

    public async Task<Result<PaymentStatusResult>> CaptureApprovedOrderAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
    {
        if (RemainingPaymentProviderShared.IsLocalSandbox(_environment, account))
        {
            var raw = new JsonObject
            {
                ["id"] = payment.ProviderPaymentId,
                ["status"] = "COMPLETED",
                ["purchase_units"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["reference_id"] = payment.Id.ToString("N"),
                        ["custom_id"] = payment.OrderId.ToString("N"),
                        ["payments"] = new JsonObject
                        {
                            ["captures"] = new JsonArray
                            {
                                new JsonObject
                                {
                                    ["id"] = $"local-paypal-capture-{payment.Id:N}",
                                    ["status"] = "COMPLETED",
                                    ["amount"] = new JsonObject
                                    {
                                        ["value"] = payment.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                                        ["currency_code"] = payment.Currency.ToUpperInvariant()
                                    }
                                }
                            }
                        }
                    }
                }
            }.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return ParseOrderStatus(payment, raw);
        }

        var token = await GetAccessTokenAsync(account, cancellationToken);
        string? captureFailure = null;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BuildPayPalUri(account, $"/v2/checkout/orders/{WebUtility.UrlEncode(payment.ProviderPaymentId)}/capture"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("PayPal-Request-Id", $"capture-{payment.Id:N}");
            request.Headers.TryAddWithoutValidation("Prefer", "return=representation");
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            using var response = await SendPayPalAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var parsed = ParseOrderStatus(payment, raw);
                if (!parsed.IsSuccess || parsed.Value is null)
                {
                    if (!parsed.IsRetryable)
                    {
                        return parsed;
                    }

                    captureFailure = parsed.Error;
                }
                else if (parsed.Value.Status != PaymentStatus.WaitingConfirmation
                    || string.Equals(parsed.Value.StatusReason, "PENDING", StringComparison.OrdinalIgnoreCase))
                {
                    return parsed;
                }
                else
                {
                    captureFailure = "PayPal accepted the capture request but did not return capture proof.";
                }
            }
            else
            {
                captureFailure = $"PayPal order capture failed with HTTP {(int)response.StatusCode}.";
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            captureFailure = $"PayPal order capture outcome is unknown: {ex.Message}";
        }

        var reconciliation = await GetOrderStatusWithTokenAsync(payment, account, token, cancellationToken);
        if (reconciliation.IsSuccess && reconciliation.Value is not null)
        {
            if (reconciliation.Value.Status is PaymentStatus.Succeeded or PaymentStatus.Failed or PaymentStatus.Cancelled)
            {
                return reconciliation;
            }
        }
        else if (!reconciliation.IsRetryable)
        {
            return reconciliation;
        }

        return Result<PaymentStatusResult>.Failure(captureFailure ?? reconciliation.Error ?? "PayPal order capture could not be confirmed.", isRetryable: true);
    }

    public async Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken)
    {
        if (RemainingPaymentProviderShared.IsLocalSandbox(_environment, account))
        {
            return RemainingPaymentProviderShared.LocalSandboxRefund(payment, amount, reason, "paypal");
        }

        var token = await GetAccessTokenAsync(account, cancellationToken);
        var captureId = await ResolveCaptureIdAsync(payment, account, token, cancellationToken);
        if (string.IsNullOrWhiteSpace(captureId))
        {
            throw new NotSupportedException("PayPal refund requires a completed order with capture id.");
        }

        var body = new JsonObject
        {
            ["amount"] = new JsonObject
            {
                ["value"] = amount.ToString("0.00", CultureInfo.InvariantCulture),
                ["currency_code"] = payment.Currency.ToUpperInvariant()
            },
            ["note_to_payer"] = string.IsNullOrWhiteSpace(reason) ? $"Refund for order {payment.OrderId}" : reason
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildPayPalUri(account, $"/v2/payments/captures/{WebUtility.UrlEncode(captureId)}/refund"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("PayPal-Request-Id", $"refund-{payment.Id:N}-{RemainingPaymentProviderShared.ToMinorUnits(amount, payment.Currency)}-{ComputeSha256Hex(reason)[..16]}");
        request.Headers.Add("Prefer", "return=representation");
        request.Content = new StringContent(body.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8, "application/json");
        using var response = await SendPayPalAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"PayPal refund failed with HTTP {(int)response.StatusCode}.");
        }

        return ParseRefundResult(raw, captureId);
    }

    public async Task<PaymentRefundResult> GetRefundStatusAsync(
        PaymentAttempt payment,
        PaymentProviderAccount account,
        Refund refund,
        CancellationToken cancellationToken)
    {
        if (RemainingPaymentProviderShared.IsLocalSandbox(_environment, account))
        {
            return new PaymentRefundResult(refund.ProviderRefundId, refund.Status, refund.RawResponse, Amount: refund.Amount, Currency: refund.Currency);
        }

        var token = await GetAccessTokenAsync(account, cancellationToken);
        var captureId = await ResolveCaptureIdAsync(payment, account, token, cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildPayPalUri(account, $"/v2/payments/refunds/{WebUtility.UrlEncode(refund.ProviderRefundId)}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await SendPayPalAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new PaymentRefundResult(refund.ProviderRefundId, RefundStatus.Unknown, raw, StatusReason: $"http_{(int)response.StatusCode}");
        }

        return ParseRefundResult(raw, captureId);
    }

    public PaymentStatus MapPaymentStatus(string providerStatus, bool paid)
        => providerStatus.Trim().ToUpperInvariant() switch
        {
            "COMPLETED" or "PAYMENT.CAPTURE.COMPLETED" or "CHECKOUT.ORDER.COMPLETED" => PaymentStatus.Succeeded,
            "APPROVED" => PaymentStatus.WaitingConfirmation,
            "CREATED" => PaymentStatus.Pending,
            "VOIDED" or "CANCELLED" => PaymentStatus.Cancelled,
            "DECLINED" or "FAILED" => PaymentStatus.Failed,
            _ => PaymentStatus.Unknown
        };

    public RefundStatus MapRefundStatus(string providerStatus)
        => providerStatus.Trim().ToUpperInvariant() switch
        {
            "COMPLETED" => RefundStatus.Succeeded,
            "PENDING" => RefundStatus.Pending,
            "FAILED" => RefundStatus.Failed,
            "CANCELLED" => RefundStatus.Cancelled,
            _ => RefundStatus.Unknown
        };

    private PaymentRefundResult ParseRefundResult(string raw, string expectedCaptureId)
    {
        using var document = JsonDocument.Parse(raw);
        var root = document.RootElement;
        var id = root.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? string.Empty : string.Empty;
        var status = root.TryGetProperty("status", out var statusElement) ? MapRefundStatus(statusElement.GetString() ?? string.Empty) : RefundStatus.Unknown;
        var refundAmount = TryReadPayPalMoney(root, "amount", out var currency);
        var responseCaptureId = TryGetPayPalUpCaptureId(root);
        var statusReason = status != RefundStatus.Unknown
            && (string.IsNullOrWhiteSpace(id) || !refundAmount.HasValue || string.IsNullOrWhiteSpace(currency) || string.IsNullOrWhiteSpace(responseCaptureId))
                ? "paypal_refund_proof_incomplete"
                : null;
        if (statusReason is not null)
        {
            status = RefundStatus.Unknown;
        }

        return new PaymentRefundResult(
            id,
            status,
            raw,
            ProviderPaymentId: responseCaptureId,
            Amount: refundAmount,
            Currency: currency,
            StatusReason: statusReason,
            ExpectedProviderPaymentId: expectedCaptureId);
    }

    private async Task<string> GetAccessTokenAsync(PaymentProviderAccount account, CancellationToken cancellationToken)
    {
        var clientId = account.ShopId;
        var secret = _accounts.GetSecretKey(account);
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("PayPal client id and secret are required.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildPayPalUri(account, "/v1/oauth2/token"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secret}")));
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials" });
        using var response = await SendPayPalAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"PayPal OAuth failed with HTTP {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(raw);
        return document.RootElement.TryGetProperty("access_token", out var tokenElement) ? tokenElement.GetString() ?? string.Empty : string.Empty;
    }

    private async Task<Result<PaymentStatusResult>> GetOrderStatusWithTokenAsync(PaymentAttempt payment, PaymentProviderAccount account, string token, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, BuildPayPalUri(account, $"/v2/checkout/orders/{WebUtility.UrlEncode(payment.ProviderPaymentId)}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await SendPayPalAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            return response.IsSuccessStatusCode
                ? ParseOrderStatus(payment, raw)
                : Result<PaymentStatusResult>.Failure($"PayPal order reconciliation failed with HTTP {(int)response.StatusCode}.", isRetryable: true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<PaymentStatusResult>.Failure($"PayPal order reconciliation failed: {ex.Message}", isRetryable: true);
        }
    }

    private static Result<PaymentStatusResult> ParseOrderStatus(PaymentAttempt payment, string raw)
    {
        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Result<PaymentStatusResult>.Failure("PayPal order response is not an object.", isRetryable: true);
            }

            var orderId = root.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.String
                ? idElement.GetString() ?? string.Empty
                : string.Empty;
            if (!string.Equals(orderId, payment.ProviderPaymentId, StringComparison.Ordinal))
            {
                return Result<PaymentStatusResult>.Failure("PayPal order id does not match payment attempt.");
            }

            var orderStatus = root.TryGetProperty("status", out var orderStatusElement) && orderStatusElement.ValueKind == JsonValueKind.String
                ? orderStatusElement.GetString() ?? string.Empty
                : string.Empty;
            var captures = new List<(string Id, string Status, decimal Amount, string Currency)>();
            if (root.TryGetProperty("purchase_units", out var units) && units.ValueKind == JsonValueKind.Array)
            {
                foreach (var unit in units.EnumerateArray())
                {
                    if (unit.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    if (unit.TryGetProperty("reference_id", out var referenceId)
                        && referenceId.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(referenceId.GetString())
                        && !string.Equals(referenceId.GetString(), payment.Id.ToString("N"), StringComparison.OrdinalIgnoreCase))
                    {
                        return Result<PaymentStatusResult>.Failure("PayPal capture reference id does not match payment attempt.");
                    }

                    if (unit.TryGetProperty("custom_id", out var customId)
                        && customId.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(customId.GetString())
                        && !string.Equals(customId.GetString(), payment.OrderId.ToString("N"), StringComparison.OrdinalIgnoreCase))
                    {
                        return Result<PaymentStatusResult>.Failure("PayPal capture custom id does not match order.");
                    }

                    if (!unit.TryGetProperty("payments", out var payments)
                        || payments.ValueKind != JsonValueKind.Object
                        || !payments.TryGetProperty("captures", out var captureArray)
                        || captureArray.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var capture in captureArray.EnumerateArray())
                    {
                        if (capture.ValueKind != JsonValueKind.Object
                            || !capture.TryGetProperty("id", out var captureIdElement)
                            || captureIdElement.ValueKind != JsonValueKind.String
                            || string.IsNullOrWhiteSpace(captureIdElement.GetString())
                            || !capture.TryGetProperty("status", out var statusElement)
                            || statusElement.ValueKind != JsonValueKind.String
                            || !capture.TryGetProperty("amount", out var amountElement)
                            || amountElement.ValueKind != JsonValueKind.Object
                            || !amountElement.TryGetProperty("value", out var valueElement)
                            || valueElement.ValueKind != JsonValueKind.String
                            || !decimal.TryParse(valueElement.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
                            || !amountElement.TryGetProperty("currency_code", out var currencyElement)
                            || currencyElement.ValueKind != JsonValueKind.String)
                        {
                            continue;
                        }

                        captures.Add((captureIdElement.GetString() ?? string.Empty, statusElement.GetString() ?? string.Empty, amount, currencyElement.GetString() ?? string.Empty));
                    }
                }
            }

            var completedCaptures = captures.Where(x => string.Equals(x.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase)).ToList();
            if (completedCaptures.Count > 0)
            {
                var currencyMatches = completedCaptures.All(x => string.Equals(x.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase));
                var total = completedCaptures.Sum(x => x.Amount);
                if (!currencyMatches || decimal.Round(total, 2) != decimal.Round(payment.Amount, 2))
                {
                    return Result<PaymentStatusResult>.Failure("PayPal completed capture amount or currency does not match payment attempt.");
                }

                return Result<PaymentStatusResult>.Success(new PaymentStatusResult(payment.ProviderPaymentId, PaymentStatus.Succeeded, raw, "COMPLETED"));
            }

            var pendingCaptures = captures.Where(x => string.Equals(x.Status, "PENDING", StringComparison.OrdinalIgnoreCase)).ToList();
            if (pendingCaptures.Count > 0)
            {
                var currencyMatches = pendingCaptures.All(x => string.Equals(x.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase));
                var total = pendingCaptures.Sum(x => x.Amount);
                if (!currencyMatches || decimal.Round(total, 2) != decimal.Round(payment.Amount, 2))
                {
                    return Result<PaymentStatusResult>.Failure("PayPal pending capture amount or currency does not match payment attempt.");
                }

                return Result<PaymentStatusResult>.Success(new PaymentStatusResult(payment.ProviderPaymentId, PaymentStatus.WaitingConfirmation, raw, "PENDING"));
            }

            var mapped = orderStatus.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase)
                ? PaymentStatus.Unknown
                : orderStatus.Equals("APPROVED", StringComparison.OrdinalIgnoreCase)
                    ? PaymentStatus.WaitingConfirmation
                    : orderStatus.Equals("CREATED", StringComparison.OrdinalIgnoreCase)
                        ? PaymentStatus.Pending
                        : orderStatus.Equals("VOIDED", StringComparison.OrdinalIgnoreCase)
                            ? PaymentStatus.Cancelled
                            : PaymentStatus.Unknown;
            return mapped == PaymentStatus.Unknown
                ? Result<PaymentStatusResult>.Failure($"PayPal order status {orderStatus} has no confirmed capture.", isRetryable: true)
                : Result<PaymentStatusResult>.Success(new PaymentStatusResult(payment.ProviderPaymentId, mapped, raw, orderStatus));
        }
        catch (JsonException ex)
        {
            return Result<PaymentStatusResult>.Failure($"PayPal order response is malformed: {ex.Message}", isRetryable: true);
        }
    }

    private async Task<string> ResolveCaptureIdAsync(PaymentAttempt payment, PaymentProviderAccount account, string token, CancellationToken cancellationToken)
    {
        static string Extract(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || !raw.TrimStart().StartsWith('{')) return string.Empty;
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return string.Empty;
                var root = doc.RootElement.TryGetProperty("resource", out var resource) && resource.ValueKind == JsonValueKind.Object
                    ? resource
                    : doc.RootElement;
                if (root.TryGetProperty("purchase_units", out var units) && units.ValueKind == JsonValueKind.Array)
                {
                    foreach (var unit in units.EnumerateArray())
                    {
                        if (unit.ValueKind == JsonValueKind.Object
                            && unit.TryGetProperty("payments", out var payments)
                            && payments.ValueKind == JsonValueKind.Object
                            && payments.TryGetProperty("captures", out var captures)
                            && captures.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var capture in captures.EnumerateArray())
                            {
                                if (capture.ValueKind == JsonValueKind.Object
                                    && capture.TryGetProperty("id", out var id)
                                    && id.ValueKind == JsonValueKind.String) return id.GetString() ?? string.Empty;
                            }
                        }
                    }
                }
                if (root.TryGetProperty("id", out var directId)
                    && directId.ValueKind == JsonValueKind.String
                    && root.TryGetProperty("status", out var status)
                    && status.ValueKind == JsonValueKind.String
                    && string.Equals(status.GetString(), "COMPLETED", StringComparison.OrdinalIgnoreCase)) return directId.GetString() ?? string.Empty;
                return string.Empty;
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }

        var fromStored = Extract(payment.WebhookPayload) is { Length: > 0 } stored ? stored : Extract(payment.RawResponse);
        if (!string.IsNullOrWhiteSpace(fromStored)) return fromStored;
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildPayPalUri(account, $"/v2/checkout/orders/{WebUtility.UrlEncode(payment.ProviderPaymentId)}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await SendPayPalAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        return response.IsSuccessStatusCode ? Extract(raw) : string.Empty;
    }

    private Task<HttpResponseMessage> SendPayPalAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => _httpClientFactory.CreateClient("PayPal").SendAsync(request, cancellationToken);

    private static Uri BuildPayPalUri(PaymentProviderAccount account, string path)
    {
        var defaultBase = account.Mode == PaymentProviderMode.Sandbox ? "https://api-m.sandbox.paypal.com" : "https://api-m.paypal.com";
        var baseUrl = string.IsNullOrWhiteSpace(account.ApiBaseUrl) ? defaultBase : account.ApiBaseUrl.TrimEnd('/');
        return new Uri($"{baseUrl}/{path.TrimStart('/')}", UriKind.Absolute);
    }

    private static decimal? TryReadPayPalMoney(JsonElement root, string propertyName, out string? currency)
    {
        currency = null;
        if (!root.TryGetProperty(propertyName, out var money) || money.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        currency = money.TryGetProperty("currency_code", out var currencyElement) && currencyElement.ValueKind == JsonValueKind.String
            ? currencyElement.GetString()?.Trim().ToUpperInvariant()
            : null;
        return money.TryGetProperty("value", out var valueElement)
            && valueElement.ValueKind == JsonValueKind.String
            && decimal.TryParse(valueElement.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
    }

    private static string? TryGetPayPalUpCaptureId(JsonElement root)
    {
        if (!root.TryGetProperty("links", out var links) || links.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var link in links.EnumerateArray())
        {
            if (link.ValueKind != JsonValueKind.Object
                || !link.TryGetProperty("rel", out var rel)
                || !string.Equals(rel.GetString(), "up", StringComparison.OrdinalIgnoreCase)
                || !link.TryGetProperty("href", out var href)
                || !Uri.TryCreate(href.GetString(), UriKind.Absolute, out var uri))
            {
                continue;
            }

            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            for (var index = 0; index + 1 < segments.Length; index++)
            {
                if (string.Equals(segments[index], "captures", StringComparison.OrdinalIgnoreCase))
                {
                    return WebUtility.UrlDecode(segments[index + 1]);
                }
            }
        }

        return null;
    }

    private static string ResolvePayPalOrderId(JsonElement resource)
    {
        if (resource.TryGetProperty("supplementary_data", out var supplementary)
            && supplementary.TryGetProperty("related_ids", out var related)
            && related.TryGetProperty("order_id", out var orderId))
        {
            return orderId.GetString() ?? string.Empty;
        }

        if (resource.TryGetProperty("id", out var id)) return id.GetString() ?? string.Empty;
        return string.Empty;
    }
}

public sealed class TBankAcquiringPaymentProvider : IPaymentProvider, IPaymentWebhookVerifier, IPaymentStatusMapper
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PaymentProviderAccountService _accounts;
    private readonly IHostEnvironment _environment;

    public TBankAcquiringPaymentProvider(IHttpClientFactory httpClientFactory, PaymentProviderAccountService accounts, IHostEnvironment environment)
    {
        _httpClientFactory = httpClientFactory;
        _accounts = accounts;
        _environment = environment;
    }

    public PaymentProvider Provider => PaymentProvider.TBankAcquiring;

    public async Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
    {
        EnsureEnabled(request.Account, Provider);
        if (RemainingPaymentProviderShared.IsLocalSandbox(_environment, request.Account))
        {
            return RemainingPaymentProviderShared.LocalSandboxInit(request, "tbank", Provider);
        }

        var password = _accounts.GetSecretKey(request.Account);
        if (string.IsNullOrWhiteSpace(request.Account.ShopId) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("T-Bank TerminalKey and password are required.");
        }

        var amountMinor = RemainingPaymentProviderShared.ToMinorUnits(request.Order.Amount, request.Order.Currency);
        var root = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["TerminalKey"] = request.Account.ShopId,
            ["Amount"] = amountMinor.ToString(CultureInfo.InvariantCulture),
            ["OrderId"] = request.Payment.Id.ToString("N"),
            ["Description"] = RemainingPaymentProviderShared.SafePaymentDescription(request.Order, 140),
            ["SuccessURL"] = AppendQuery(request.ReturnUrl, new Dictionary<string, string> { ["provider"] = "TBankAcquiring", ["status"] = "success" }),
            ["FailURL"] = AppendQuery(request.ReturnUrl, new Dictionary<string, string> { ["provider"] = "TBankAcquiring", ["status"] = "fail" })
        };
        var notificationUrl = RemainingPaymentProviderShared.GetExtra(request.Account, "notificationUrl");
        if (!string.IsNullOrWhiteSpace(notificationUrl)) root["NotificationURL"] = notificationUrl;
        root["Token"] = BuildTBankToken(root, password);
        var body = new JsonObject
        {
            ["TerminalKey"] = root["TerminalKey"],
            ["Amount"] = long.Parse(root["Amount"], CultureInfo.InvariantCulture),
            ["OrderId"] = root["OrderId"],
            ["Description"] = root["Description"],
            ["SuccessURL"] = root["SuccessURL"],
            ["FailURL"] = root["FailURL"],
            ["Token"] = root["Token"]
        };
        if (!string.IsNullOrWhiteSpace(notificationUrl)) body["NotificationURL"] = notificationUrl;
        var rawRequest = body.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        request.Payment.RawRequest = SecretRedactor.Redact(rawRequest, new[] { password });
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildTBankUri(request.Account, "/v2/Init"));
        httpRequest.Content = new StringContent(rawRequest, Encoding.UTF8, "application/json");
        using var response = await _httpClientFactory.CreateClient("TBankAcquiring").SendAsync(httpRequest, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"T-Bank Init failed with HTTP {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(raw);
        var responseRoot = document.RootElement;
        var success = responseRoot.TryGetProperty("Success", out var successElement) && successElement.ValueKind == JsonValueKind.True;
        if (!success)
        {
            var message = responseRoot.TryGetProperty("Message", out var msg) ? msg.GetString() : "unknown";
            throw new InvalidOperationException($"T-Bank Init failed: {message}");
        }
        var paymentId = responseRoot.TryGetProperty("PaymentId", out var id) ? id.ToString() : string.Empty;
        var url = responseRoot.TryGetProperty("PaymentURL", out var urlElement) ? urlElement.GetString() ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(paymentId) || string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("T-Bank Init response does not contain PaymentId/PaymentURL.");
        }

        return new PaymentInitResult(paymentId, url, raw);
    }

    public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var values = RemainingPaymentProviderShared.ParseJsonOrForm(rawBody);
        var paymentId = GetFirst(values, "PaymentId");
        var statusText = GetFirst(values, "Status");
        var success = ParseBool(GetFirst(values, "Success"));
        var amountText = GetFirst(values, "Amount");
        var amount = long.TryParse(amountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minor) ? RemainingPaymentProviderShared.FromMinorUnits(minor, "RUB") : (decimal?)null;
        var terminal = GetFirst(values, "TerminalKey");
        var orderId = GetFirst(values, "OrderId");
        var status = MapPaymentStatus(statusText, success);
        if (status == PaymentStatus.Succeeded && !amount.HasValue)
        {
            status = PaymentStatus.Unknown;
        }
        var externalEventId = $"tbank:{paymentId}:{statusText}:{ComputeSha256Hex(rawBody)[..12]}";
        return Task.FromResult(new PaymentWebhookParseResult(externalEventId, statusText, paymentId, status, rawBody, false, amount, "RUB", status == PaymentStatus.Succeeded, terminal, orderId));
    }

    public Task<PaymentWebhookVerificationResult> VerifyAsync(PaymentProviderAccount account, PaymentWebhookParseResult parsed, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var local = RemainingPaymentProviderShared.VerifyLocalSandboxHeader(_environment, account, headers, "X-TBank-Sandbox-Webhook");
        if (local.Method == "local-sandbox-header" || !local.IsValid)
        {
            return Task.FromResult(local);
        }

        var values = RemainingPaymentProviderShared.ParseJsonOrForm(rawBody);
        var provided = GetFirst(values, "Token");
        if (string.IsNullOrWhiteSpace(provided))
        {
            return Task.FromResult(new PaymentWebhookVerificationResult(false, "tbank-token", "Missing T-Bank Token."));
        }

        var password = _accounts.GetSecretKey(account);
        if (string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult(new PaymentWebhookVerificationResult(false, "tbank-token", "T-Bank terminal password is required."));
        }

        var expected = BuildTBankToken(values.Where(x => !x.Key.Contains('.', StringComparison.Ordinal) && !x.Key.Contains('[', StringComparison.Ordinal)).ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal), password);
        var valid = FixedEqualsHex(provided, expected);
        return Task.FromResult(valid
            ? new PaymentWebhookVerificationResult(true, "tbank-token", null)
            : new PaymentWebhookVerificationResult(false, "tbank-token", "Invalid T-Bank Token."));
    }

    public async Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
    {
        if (RemainingPaymentProviderShared.IsLocalSandbox(_environment, account))
        {
            return new PaymentStatusResult(payment.ProviderPaymentId, payment.Status, payment.RawResponse, payment.StatusReason);
        }

        var password = _accounts.GetSecretKey(account);
        if (string.IsNullOrWhiteSpace(account.ShopId) || string.IsNullOrWhiteSpace(password))
        {
            return new PaymentStatusResult(payment.ProviderPaymentId, PaymentStatus.Unknown, "{}", "tbank_credentials_missing");
        }

        var values = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["TerminalKey"] = account.ShopId,
            ["PaymentId"] = payment.ProviderPaymentId
        };
        values["Token"] = BuildTBankToken(values, password);
        var body = JsonSerializer.Serialize(values, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildTBankUri(account, "/v2/GetState"));
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await _httpClientFactory.CreateClient("TBankAcquiring").SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new PaymentStatusResult(payment.ProviderPaymentId, PaymentStatus.Unknown, raw, $"http_{(int)response.StatusCode}");
        }

        using var document = JsonDocument.Parse(raw);
        var root = document.RootElement;
        var responsePaymentId = root.TryGetProperty("PaymentId", out var paymentIdElement) ? paymentIdElement.ToString() : string.Empty;
        var statusText = root.TryGetProperty("Status", out var statusElement) ? statusElement.GetString() ?? string.Empty : string.Empty;
        var success = root.TryGetProperty("Success", out var successElement) && successElement.ValueKind == JsonValueKind.True;
        var amount = root.TryGetProperty("Amount", out var amountElement) && amountElement.TryGetInt64(out var amountMinor)
            ? RemainingPaymentProviderShared.FromMinorUnits(amountMinor, payment.Currency)
            : (decimal?)null;
        var orderId = root.TryGetProperty("OrderId", out var orderIdElement) ? orderIdElement.ToString() : null;
        var terminalKey = root.TryGetProperty("TerminalKey", out var terminalElement) ? terminalElement.GetString() : null;
        var status = MapPaymentStatus(statusText, success);
        var statusReason = status == PaymentStatus.Succeeded
            && (string.IsNullOrWhiteSpace(responsePaymentId) || !amount.HasValue || string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(terminalKey))
                ? "tbank_payment_proof_incomplete"
                : statusText;
        if (statusReason == "tbank_payment_proof_incomplete")
        {
            status = PaymentStatus.Unknown;
        }
        return new PaymentStatusResult(
            responsePaymentId,
            status,
            raw,
            statusReason,
            amount,
            null,
            orderId,
            terminalKey,
            success);
    }

    public async Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken)
    {
        if (RemainingPaymentProviderShared.IsLocalSandbox(_environment, account))
        {
            return RemainingPaymentProviderShared.LocalSandboxRefund(payment, amount, reason, "tbank");
        }

        var password = _accounts.GetSecretKey(account);
        if (string.IsNullOrWhiteSpace(account.ShopId) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("T-Bank TerminalKey and password are required.");
        }

        var values = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["TerminalKey"] = account.ShopId,
            ["PaymentId"] = payment.ProviderPaymentId,
            ["Amount"] = RemainingPaymentProviderShared.ToMinorUnits(amount, payment.Currency).ToString(CultureInfo.InvariantCulture)
        };
        values["Token"] = BuildTBankToken(values, password);
        var body = JsonSerializer.Serialize(values, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildTBankUri(account, "/v2/Cancel"));
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await _httpClientFactory.CreateClient("TBankAcquiring").SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"T-Bank Cancel/refund failed with HTTP {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(raw);
        var root = document.RootElement;
        var success = root.TryGetProperty("Success", out var successElement) && successElement.ValueKind == JsonValueKind.True;
        var responsePaymentId = root.TryGetProperty("PaymentId", out var idElement) ? idElement.ToString() : string.Empty;
        var statusText = root.TryGetProperty("Status", out var statusElement) ? statusElement.GetString() ?? string.Empty : string.Empty;
        var status = success ? MapTBankRefundStatus(statusText) : RefundStatus.Failed;
        var providerRefundId = string.IsNullOrWhiteSpace(responsePaymentId)
            ? string.Empty
            : $"tbank-refund:{responsePaymentId}:{RemainingPaymentProviderShared.ToMinorUnits(amount, payment.Currency)}:{ComputeSha256Hex(reason)[..16]}";
        return new PaymentRefundResult(providerRefundId, status, raw, ProviderPaymentId: responsePaymentId, StatusReason: statusText);
    }

    public PaymentStatus MapPaymentStatus(string providerStatus, bool paid)
        => (providerStatus.Trim().ToUpperInvariant(), paid) switch
        {
            ("CONFIRMED", true) => PaymentStatus.Succeeded,
            ("CONFIRMED", false) => PaymentStatus.Unknown,
            ("AUTHORIZED", _) => PaymentStatus.WaitingConfirmation,
            ("NEW" or "FORM_SHOWED" or "3DS_CHECKING", _) => PaymentStatus.Pending,
            ("DEADLINE_EXPIRED" or "CANCELED", _) => PaymentStatus.Cancelled,
            ("REJECTED", _) => PaymentStatus.Failed,
            _ => PaymentStatus.Unknown
        };

    public RefundStatus MapRefundStatus(string providerStatus) => MapTBankRefundStatus(providerStatus);

    private static RefundStatus MapTBankRefundStatus(string providerStatus)
        => providerStatus.Trim().ToUpperInvariant() switch
        {
            "REFUNDED" or "PARTIAL_REFUNDED" or "REVERSED" or "PARTIAL_REVERSED" => RefundStatus.Succeeded,
            "NEW" or "FORM_SHOWED" or "AUTHORIZING" => RefundStatus.Pending,
            "REJECTED" or "DEADLINE_EXPIRED" => RefundStatus.Failed,
            "CANCELED" => RefundStatus.Cancelled,
            _ => RefundStatus.Unknown
        };

    public static string BuildTBankToken(IReadOnlyDictionary<string, string> values, string password)
    {
        var all = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in values)
        {
            if (string.Equals(pair.Key, "Token", StringComparison.OrdinalIgnoreCase)) continue;
            if (pair.Key.Contains('.', StringComparison.Ordinal) || pair.Key.Contains('[', StringComparison.Ordinal)) continue;
            all[pair.Key] = pair.Value;
        }
        all["Password"] = password;
        var concatenated = string.Concat(all.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Value));
        return ComputeSha256Hex(concatenated);
    }

    private static Uri BuildTBankUri(PaymentProviderAccount account, string path)
    {
        var baseUrl = string.IsNullOrWhiteSpace(account.ApiBaseUrl) ? "https://securepay.tinkoff.ru" : account.ApiBaseUrl.TrimEnd('/');
        return new Uri($"{baseUrl}/{path.TrimStart('/')}", UriKind.Absolute);
    }
}

public sealed class CloudPaymentsPaymentProvider : IPaymentProvider, IPaymentWebhookVerifier, IPaymentStatusMapper
{
    private readonly PaymentProviderAccountService _accounts;
    private readonly IHostEnvironment _environment;

    public CloudPaymentsPaymentProvider(PaymentProviderAccountService accounts, IHostEnvironment environment)
    {
        _accounts = accounts;
        _environment = environment;
    }

    public PaymentProvider Provider => PaymentProvider.CloudPayments;

    public Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
    {
        EnsureEnabled(request.Account, Provider);
        if (RemainingPaymentProviderShared.IsLocalSandbox(_environment, request.Account))
        {
            return Task.FromResult(RemainingPaymentProviderShared.LocalSandboxInit(request, "cp", Provider));
        }

        if (string.IsNullOrWhiteSpace(request.Account.ShopId))
        {
            throw new InvalidOperationException("CloudPayments PublicId is required in ShopId.");
        }

        var hostedCheckoutUrl = RemainingPaymentProviderShared.GetExtra(request.Account, "hostedCheckoutUrl");
        if (string.IsNullOrWhiteSpace(hostedCheckoutUrl))
        {
            throw new NotSupportedException("CloudPayments requires a merchant-hosted widget checkout URL in ExtraSettingsJson.hostedCheckoutUrl. Server-side fake redirects are not allowed.");
        }

        var invoiceId = $"cp_{request.Payment.Id:N}";
        var parameters = new Dictionary<string, string>
        {
            ["publicId"] = request.Account.ShopId,
            ["description"] = RemainingPaymentProviderShared.SafePaymentDescription(request.Order),
            ["amount"] = request.Order.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            ["currency"] = request.Order.Currency.ToUpperInvariant(),
            ["invoiceId"] = invoiceId,
            ["accountId"] = request.Order.UserId.ToString("N"),
            ["returnUrl"] = request.ReturnUrl
        };
        var url = AppendQuery(hostedCheckoutUrl, parameters);
        request.Payment.RawRequest = RemainingPaymentProviderShared.SerializeRaw(new { provider = "CloudPayments", widget = parameters });
        var raw = RemainingPaymentProviderShared.SerializeRaw(new { invoiceId, url, provider = "CloudPayments", mode = request.Account.Mode.ToString() });
        return Task.FromResult(new PaymentInitResult(invoiceId, url, raw));
    }

    public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var values = RemainingPaymentProviderShared.ParseJsonOrForm(rawBody);
        var invoiceId = GetFirst(values, "InvoiceId", "invoiceId");
        var transactionId = GetFirst(values, "TransactionId", "transactionId");
        var amount = decimal.TryParse(GetFirst(values, "Amount", "amount"), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedAmount) ? parsedAmount : (decimal?)null;
        var currency = RemainingPaymentProviderShared.NormalizeProviderCurrency(GetFirst(values, "Currency", "currency"));
        var eventType = headers.TryGetValue("X-CloudPayments-Event", out var headerEvent) ? headerEvent : GetFirst(values, "Event", "event", "NotificationType");
        if (string.IsNullOrWhiteSpace(eventType))
        {
            eventType = string.IsNullOrWhiteSpace(transactionId) ? "unknown" : "pay";
        }
        var status = MapPaymentStatus(eventType, false);
        if (status == PaymentStatus.Succeeded && (!amount.HasValue || string.IsNullOrWhiteSpace(currency)))
        {
            status = PaymentStatus.Unknown;
        }
        var externalEventId = string.IsNullOrWhiteSpace(transactionId) ? $"cloudpayments:{invoiceId}:{ComputeSha256Hex(rawBody)[..16]}" : $"cloudpayments:{transactionId}";
        return Task.FromResult(new PaymentWebhookParseResult(externalEventId, eventType, invoiceId, status, rawBody, false, amount, currency, status == PaymentStatus.Succeeded, null, null));
    }

    public Task<PaymentWebhookVerificationResult> VerifyAsync(PaymentProviderAccount account, PaymentWebhookParseResult parsed, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var local = RemainingPaymentProviderShared.VerifyLocalSandboxHeader(_environment, account, headers, "X-CloudPayments-Sandbox-Webhook");
        if (local.Method == "local-sandbox-header" || !local.IsValid)
        {
            return Task.FromResult(local);
        }

        var secret = _accounts.GetSecretKey(account);
        var provided = headers.TryGetValue("Content-HMAC", out var contentHmac) ? contentHmac : headers.TryGetValue("X-Content-HMAC", out var xContentHmac) ? xContentHmac : string.Empty;
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(provided))
        {
            return Task.FromResult(new PaymentWebhookVerificationResult(false, "cloudpayments-content-hmac", "CloudPayments HMAC secret/header is missing."));
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        var valid = RemainingPaymentProviderShared.FixedEqualsBase64(provided, expected);
        return Task.FromResult(valid
            ? new PaymentWebhookVerificationResult(true, "cloudpayments-content-hmac", null)
            : new PaymentWebhookVerificationResult(false, "cloudpayments-content-hmac", "Invalid CloudPayments notification HMAC."));
    }

    public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
        => throw new NotSupportedException("CloudPayments manual recheck is fail-closed until API credentials/method mapping is configured for this merchant.");

    public Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken)
        => throw new NotSupportedException("CloudPayments refund is fail-closed in this widget adapter; add API refund credentials before enabling refunds.");

    public PaymentStatus MapPaymentStatus(string providerStatus, bool paid)
        => providerStatus.Trim().ToLowerInvariant() switch
        {
            "pay" or "confirm" or "paid" or "completed" => PaymentStatus.Succeeded,
            "check" => PaymentStatus.Pending,
            "fail" => PaymentStatus.Failed,
            "cancel" => PaymentStatus.Cancelled,
            "refund" => PaymentStatus.Refunded,
            _ => PaymentStatus.Unknown
        };

    public RefundStatus MapRefundStatus(string providerStatus) => providerStatus == "refund" ? RefundStatus.Succeeded : RefundStatus.Unknown;
}

public sealed class ProdamusPaymentProvider : IPaymentProvider, IPaymentWebhookVerifier, IPaymentStatusMapper
{
    private readonly PaymentProviderAccountService _accounts;
    private readonly IHostEnvironment _environment;

    public ProdamusPaymentProvider(PaymentProviderAccountService accounts, IHostEnvironment environment)
    {
        _accounts = accounts;
        _environment = environment;
    }

    public PaymentProvider Provider => PaymentProvider.Prodamus;

    public Task<PaymentInitResult> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken cancellationToken)
    {
        EnsureEnabled(request.Account, Provider);
        if (RemainingPaymentProviderShared.IsLocalSandbox(_environment, request.Account))
        {
            return Task.FromResult(RemainingPaymentProviderShared.LocalSandboxInit(request, "pd", Provider));
        }

        var formUrl = string.IsNullOrWhiteSpace(request.Account.ApiBaseUrl) ? request.Account.ReturnUrl : request.Account.ApiBaseUrl;
        var secret = _accounts.GetSecretKey(request.Account);
        if (string.IsNullOrWhiteSpace(formUrl) || string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("Prodamus payform URL and secret key are required.");
        }

        var providerPaymentId = $"pd_{request.Payment.Id:N}";
        var data = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["do"] = "pay",
            ["order_id"] = providerPaymentId,
            ["customer_extra"] = RemainingPaymentProviderShared.SafePaymentDescription(request.Order),
            ["products[0][name]"] = RemainingPaymentProviderShared.SafePaymentDescription(request.Order, 80),
            ["products[0][price]"] = request.Order.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            ["products[0][quantity]"] = "1",
            ["products[0][sku]"] = request.Order.TariffId.ToString("N"),
            ["urlSuccess"] = AppendQuery(request.ReturnUrl, new Dictionary<string, string> { ["provider"] = "Prodamus", ["status"] = "success" }),
            ["urlReturn"] = AppendQuery(request.ReturnUrl, new Dictionary<string, string> { ["provider"] = "Prodamus", ["status"] = "return" }),
            ["currency"] = request.Order.Currency.ToLowerInvariant()
        };
        var notificationUrl = RemainingPaymentProviderShared.GetExtra(request.Account, "notificationUrl");
        if (!string.IsNullOrWhiteSpace(notificationUrl)) data["urlNotification"] = notificationUrl;
        var sys = RemainingPaymentProviderShared.GetExtra(request.Account, "sys");
        if (!string.IsNullOrWhiteSpace(sys)) data["sys"] = sys;
        data["signature"] = BuildProdamusSignature(data, secret);
        var url = AppendQuery(formUrl, data);
        request.Payment.RawRequest = RemainingPaymentProviderShared.SerializeRaw(new { provider = "Prodamus", data = data.Where(x => x.Key != "signature").ToDictionary(x => x.Key, x => x.Value), signature = "***" });
        var raw = RemainingPaymentProviderShared.SerializeRaw(new { id = providerPaymentId, url, provider = "Prodamus" });
        return Task.FromResult(new PaymentInitResult(providerPaymentId, url, raw));
    }

    public Task<PaymentWebhookParseResult> ParseWebhookAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var values = RemainingPaymentProviderShared.ParseJsonOrForm(rawBody);
        var orderId = GetFirst(values, "order_id", "orderId");
        var transactionId = GetFirst(values, "payment_id", "transaction_id", "id");
        var amount = decimal.TryParse(GetFirst(values, "sum", "amount", "order_sum", "payment_amount"), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedAmount) ? parsedAmount : (decimal?)null;
        var currency = RemainingPaymentProviderShared.NormalizeProviderCurrency(GetFirst(values, "currency"));
        var statusText = GetFirst(values, "status", "payment_status", "paid");
        var paid = string.Equals(statusText, "success", StringComparison.OrdinalIgnoreCase) || string.Equals(statusText, "paid", StringComparison.OrdinalIgnoreCase) || ParseBool(statusText);
        var status = MapPaymentStatus(statusText, paid);
        if (status == PaymentStatus.Succeeded && (!amount.HasValue || string.IsNullOrWhiteSpace(currency)))
        {
            status = PaymentStatus.Unknown;
            paid = false;
        }
        var eventId = string.IsNullOrWhiteSpace(transactionId) ? $"prodamus:{orderId}:{ComputeSha256Hex(rawBody)[..16]}" : $"prodamus:{transactionId}";
        return Task.FromResult(new PaymentWebhookParseResult(eventId, "payment", orderId, status, rawBody, false, amount, currency, paid, null, null));
    }

    public Task<PaymentWebhookVerificationResult> VerifyAsync(PaymentProviderAccount account, PaymentWebhookParseResult parsed, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        var local = RemainingPaymentProviderShared.VerifyLocalSandboxHeader(_environment, account, headers, "X-Prodamus-Sandbox-Webhook");
        if (local.Method == "local-sandbox-header" || !local.IsValid)
        {
            return Task.FromResult(local);
        }

        var provided = headers.TryGetValue("Sign", out var sign) ? sign : headers.TryGetValue("X-Prodamus-Sign", out var xSign) ? xSign : string.Empty;
        if (string.IsNullOrWhiteSpace(provided))
        {
            return Task.FromResult(new PaymentWebhookVerificationResult(false, "prodamus-hmac", "Missing Prodamus Sign header."));
        }

        var secret = _accounts.GetWebhookSecret(account);
        if (string.IsNullOrWhiteSpace(secret)) secret = _accounts.GetSecretKey(account);
        if (string.IsNullOrWhiteSpace(secret))
        {
            return Task.FromResult(new PaymentWebhookVerificationResult(false, "prodamus-hmac", "Prodamus webhook secret is required."));
        }

        var values = RemainingPaymentProviderShared.ParseJsonOrForm(rawBody);
        var expected = BuildProdamusSignature(values, secret);
        var valid = string.Equals(provided.Trim(), expected, StringComparison.Ordinal) || FixedEqualsHex(provided, expected);
        return Task.FromResult(valid
            ? new PaymentWebhookVerificationResult(true, "prodamus-hmac", null)
            : new PaymentWebhookVerificationResult(false, "prodamus-hmac", "Invalid Prodamus signature."));
    }

    public Task<PaymentStatusResult> GetStatusAsync(PaymentAttempt payment, PaymentProviderAccount account, CancellationToken cancellationToken)
        => throw new NotSupportedException("Prodamus payform status recheck is fail-closed unless a merchant-specific REST API is configured.");

    public Task<PaymentRefundResult> RefundAsync(PaymentAttempt payment, PaymentProviderAccount account, decimal amount, string reason, CancellationToken cancellationToken)
        => throw new NotSupportedException("Prodamus refunds require merchant API configuration and are fail-closed in this adapter.");

    public PaymentStatus MapPaymentStatus(string providerStatus, bool paid)
        => providerStatus.Trim().ToLowerInvariant() switch
        {
            "success" or "paid" or "completed" => PaymentStatus.Succeeded,
            "fail" or "failed" => PaymentStatus.Failed,
            "cancel" or "cancelled" or "canceled" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Unknown
        };

    public RefundStatus MapRefundStatus(string providerStatus) => providerStatus == "refund" ? RefundStatus.Succeeded : RefundStatus.Unknown;

    public static string BuildProdamusSignature(IReadOnlyDictionary<string, string> values, string secret)
    {
        var filtered = values
            .Where(x => !string.Equals(x.Key, "signature", StringComparison.OrdinalIgnoreCase) && !string.Equals(x.Key, "sign", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => new KeyValuePair<string, string>(x.Key, x.Value ?? string.Empty));
        var json = JsonSerializer.Serialize(filtered.ToDictionary(x => x.Key, x => x.Value), new JsonSerializerOptions { WriteIndented = false }).Replace("/", "\\/", StringComparison.Ordinal);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(json)));
    }
}
