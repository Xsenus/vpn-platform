using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Security;
using VpnPlatform.Infrastructure.Services;

namespace VpnPlatform.Infrastructure.Vpn;

public class X3UiHttpClient : IX3UiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<X3UiHttpClient> _logger;
    private readonly IClock _clock;

    public X3UiHttpClient(IHttpClientFactory httpClientFactory, ILogger<X3UiHttpClient> logger, IClock? clock = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _clock = clock ?? new SystemClock();
    }

    public async Task<X3UiSession> LoginAsync(VpnPanel panel, string password, CancellationToken cancellationToken)
    {
        using var lease = CreateClient(panel);
        var client = lease.Client;
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(panel, "login"));
        request.Content = JsonContent.Create(new { username = panel.Login, password }, options: JsonOptions);
        using var response = await SendWithRetryAsync(client, request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode || !IsSuccessResponse(body))
        {
            _logger.LogWarning("3x-ui login failed for panel {PanelId} with HTTP {StatusCode}. Body={Body}", panel.Id, (int)response.StatusCode, SecretRedactor.Redact(body));
            throw new InvalidOperationException($"3x-ui login failed with HTTP {(int)response.StatusCode}.");
        }

        var cookies = response.Headers.TryGetValues("Set-Cookie", out var values) ? string.Join("; ", values.Select(x => x.Split(';')[0])) : string.Empty;
        if (string.IsNullOrWhiteSpace(cookies))
        {
            throw new InvalidOperationException("3x-ui login response did not contain a session cookie.");
        }

        return new X3UiSession(cookies, _clock.UtcNow);
    }

    public async Task<X3UiHealthResult> CheckHealthAsync(VpnPanel panel, string password, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await LoginAsync(panel, password, cancellationToken);
            var version = await GetPanelVersionAsync(panel, password, cancellationToken);
            sw.Stop();
            return new X3UiHealthResult(true, version.Version, sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new X3UiHealthResult(false, string.Empty, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    public async Task<X3UiPanelVersionResult> GetPanelVersionAsync(VpnPanel panel, string password, CancellationToken cancellationToken)
    {
        var raw = await GetRawAsync(panel, password, "server/status", cancellationToken, allowNotFound: true);
        var version = TryReadString(raw, "version");
        if (string.IsNullOrWhiteSpace(version))
        {
            version = TryReadNestedString(raw, "obj", "xray", "version");
        }
        return new X3UiPanelVersionResult(string.IsNullOrWhiteSpace(version) ? "unknown" : version, raw);
    }

    public async Task<IReadOnlyCollection<X3UiInboundDto>> GetInboundsAsync(VpnPanel panel, string password, CancellationToken cancellationToken)
    {
        var raw = await GetRawAsync(panel, password, "panel/api/inbounds/list", cancellationToken, allowArrayResponse: true);
        return ParseInbounds(raw);
    }

    public async Task<X3UiInboundDto?> GetInboundAsync(VpnPanel panel, string password, string inboundId, CancellationToken cancellationToken)
    {
        var raw = await GetRawAsync(panel, password, $"panel/api/inbounds/get/{Uri.EscapeDataString(inboundId)}", cancellationToken);
        return ParseInboundObject(raw);
    }

    public async Task<X3UiInboundDto> CreateInboundAsync(VpnPanel panel, string password, X3UiCreateInboundRequest request, CancellationToken cancellationToken)
    {
        var payload = new
        {
            up = 0,
            down = 0,
            total = 0,
            remark = request.Remark,
            enable = request.Enable,
            expiryTime = 0,
            listen = request.Listen,
            port = request.Port,
            protocol = request.Protocol,
            settings = request.SettingsJson,
            streamSettings = request.StreamSettingsJson,
            sniffing = request.SniffingJson
        };
        var raw = await PostRawAsync(panel, password, "panel/api/inbounds/add", payload, cancellationToken);
        return ParseInboundObject(raw) ?? new X3UiInboundDto(TryReadString(raw, "id"), request.Remark, request.Protocol, request.Port, request.Listen, request.SettingsJson, request.StreamSettingsJson, request.SniffingJson, request.Enable);
    }

    public async Task DeleteInboundAsync(VpnPanel panel, string password, string inboundId, CancellationToken cancellationToken)
    {
        await PostRawAsync(panel, password, $"panel/api/inbounds/del/{Uri.EscapeDataString(inboundId)}", new { }, cancellationToken, allowEmptySuccess: true);
    }

    public async Task<X3UiInboundDto> UpdateInboundAsync(VpnPanel panel, string password, X3UiUpdateInboundRequest request, CancellationToken cancellationToken)
    {
        var payload = new
        {
            id = request.Id,
            remark = request.Remark,
            enable = request.Enable,
            listen = request.Listen,
            port = request.Port,
            protocol = request.Protocol,
            settings = request.SettingsJson,
            streamSettings = request.StreamSettingsJson,
            sniffing = request.SniffingJson
        };
        var raw = await PostRawAsync(panel, password, $"panel/api/inbounds/update/{Uri.EscapeDataString(request.Id)}", payload, cancellationToken);
        return ParseInboundObject(raw) ?? new X3UiInboundDto(request.Id, request.Remark, request.Protocol, request.Port, request.Listen, request.SettingsJson, request.StreamSettingsJson, request.SniffingJson, request.Enable);
    }

    public async Task<X3UiClientDto> AddClientAsync(VpnPanel panel, string password, X3UiAddClientRequest request, CancellationToken cancellationToken)
    {
        var settings = BuildClientSettings(request.Email, request.Uuid, request.Flow, request.LimitIp, request.TotalGb, request.ExpiryTime, request.Enable);
        var payload = new { id = request.InboundId, settings };
        var raw = await PostRawAsync(panel, password, "panel/api/inbounds/addClient", payload, cancellationToken, allowEmptySuccess: true);
        return new X3UiClientDto(request.Uuid, request.Email, request.Uuid, request.Flow, request.LimitIp, request.TotalGb, request.ExpiryTime, request.Enable, null, null);
    }

    public async Task<X3UiClientDto> UpdateClientAsync(VpnPanel panel, string password, X3UiUpdateClientRequest request, CancellationToken cancellationToken)
    {
        var settings = BuildClientSettings(request.Email, request.Uuid, request.Flow, request.LimitIp, request.TotalGb, request.ExpiryTime, request.Enable);
        var payload = new { id = request.InboundId, settings };
        await PostRawAsync(panel, password, $"panel/api/inbounds/updateClient/{Uri.EscapeDataString(request.ClientId)}", payload, cancellationToken, allowEmptySuccess: true);
        return new X3UiClientDto(request.ClientId, request.Email, request.Uuid, request.Flow, request.LimitIp, request.TotalGb, request.ExpiryTime, request.Enable, null, null);
    }

    public async Task DeleteClientAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken)
    {
        await PostRawAsync(panel, password, $"panel/api/inbounds/delClient/{Uri.EscapeDataString(inboundId)}/{Uri.EscapeDataString(clientId)}", new { }, cancellationToken, allowEmptySuccess: true);
    }

    public Task EnableClientAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken)
        => SetClientEnabledAsync(panel, password, inboundId, clientId, true, cancellationToken);

    public Task DisableClientAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken)
        => SetClientEnabledAsync(panel, password, inboundId, clientId, false, cancellationToken);

    public async Task ResetClientTrafficAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken)
    {
        await PostRawAsync(panel, password, $"panel/api/inbounds/{Uri.EscapeDataString(inboundId)}/resetClientTraffic/{Uri.EscapeDataString(clientId)}", new { }, cancellationToken, allowEmptySuccess: true);
    }

    public async Task<X3UiTrafficSnapshot> GetClientTrafficAsync(VpnPanel panel, string password, string clientId, CancellationToken cancellationToken)
    {
        var raw = await GetRawAsync(panel, password, $"panel/api/inbounds/getClientTraffics/{Uri.EscapeDataString(clientId)}", cancellationToken, allowNotFound: true);
        return new X3UiTrafficSnapshot(clientId, TryReadLong(raw, "up"), TryReadLong(raw, "down"), _clock.UtcNow);
    }

    private async Task SetClientEnabledAsync(VpnPanel panel, string password, string inboundId, string clientId, bool enabled, CancellationToken cancellationToken)
    {
        var inbound = await GetInboundAsync(panel, password, inboundId, cancellationToken)
            ?? throw new InvalidOperationException("3x-ui inbound client configuration was not found.");
        var settings = BuildClientEnabledSettings(inbound.SettingsJson, clientId, enabled);
        await PostRawAsync(panel, password, $"panel/api/inbounds/updateClient/{Uri.EscapeDataString(clientId)}", new { id = inboundId, settings }, cancellationToken, allowEmptySuccess: true);
    }

    private ClientLease CreateClient(VpnPanel panel)
    {
        if (panel.SslVerificationMode == VpnSslVerificationMode.Strict)
        {
            var client = _httpClientFactory.CreateClient("X3Ui");
            return new ClientLease(client, ownsClient: false);
        }

        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            ServerCertificateCustomValidationCallback = panel.SslVerificationMode is VpnSslVerificationMode.AllowSelfSigned or VpnSslVerificationMode.Disabled
                ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                : null
        };
        var unmanagedClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(panel.BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(20)
        };
        return new ClientLease(unmanagedClient, ownsClient: true);
    }

    private async Task<string> GetRawAsync(
        VpnPanel panel,
        string password,
        string path,
        CancellationToken cancellationToken,
        bool allowNotFound = false,
        bool allowArrayResponse = false)
    {
        var session = await LoginAsync(panel, password, cancellationToken);
        using var lease = CreateClient(panel);
        var client = lease.Client;
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(panel, path));
        request.Headers.Add("Cookie", session.SessionCookie);
        using var response = await SendWithRetryAsync(client, request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode && !(allowNotFound && response.StatusCode == HttpStatusCode.NotFound))
        {
            throw new InvalidOperationException($"3x-ui GET {path} failed with HTTP {(int)response.StatusCode}.");
        }
        if (response.IsSuccessStatusCode && !IsSuccessResponse(raw, allowArrayResponse))
        {
            throw new InvalidOperationException($"3x-ui GET {path} returned unsuccessful response.");
        }
        return raw;
    }

    private async Task<string> PostRawAsync(VpnPanel panel, string password, string path, object payload, CancellationToken cancellationToken, bool allowEmptySuccess = false)
    {
        var session = await LoginAsync(panel, password, cancellationToken);
        using var lease = CreateClient(panel);
        var client = lease.Client;
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(panel, path));
        request.Headers.Add("Cookie", session.SessionCookie);
        request.Content = JsonContent.Create(payload, options: JsonOptions);
        using var response = await SendWithRetryAsync(client, request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("3x-ui POST {Path} failed for panel {PanelId} with HTTP {StatusCode}. Body={Body}", path, panel.Id, (int)response.StatusCode, SecretRedactor.Redact(raw));
            throw new InvalidOperationException($"3x-ui POST {path} failed with HTTP {(int)response.StatusCode}.");
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            if (!allowEmptySuccess)
            {
                throw new InvalidOperationException($"3x-ui POST {path} returned an empty response.");
            }

            return raw;
        }

        if (!IsSuccessResponse(raw))
        {
            throw new InvalidOperationException($"3x-ui POST {path} returned unsuccessful response.");
        }

        return raw;
    }

    private sealed class ClientLease : IDisposable
    {
        private readonly bool _ownsClient;

        public ClientLease(HttpClient client, bool ownsClient)
        {
            Client = client;
            _ownsClient = ownsClient;
        }

        public HttpClient Client { get; }

        public void Dispose()
        {
            if (_ownsClient)
            {
                Client.Dispose();
            }
        }
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(HttpClient client, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var clone = await CloneRequestAsync(request, cancellationToken);
                var response = await client.SendAsync(clone, cancellationToken);
                if ((int)response.StatusCode < 500 || attempt == 3)
                {
                    return response;
                }

                response.Dispose();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch when (attempt < 3)
            {
            }

            await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken);
        }

        throw new InvalidOperationException("3x-ui request failed after retries.");
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }

    private static Uri BuildUri(VpnPanel panel, string path)
        => new(new Uri(panel.BaseUrl.TrimEnd('/') + "/"), path.TrimStart('/'));

    private static string BuildClientSettings(string email, string uuid, string flow, int limitIp, long? totalGb, DateTimeOffset expiry, bool enable)
        => JsonSerializer.Serialize(new
        {
            clients = new[]
            {
                new
                {
                    id = uuid,
                    flow,
                    email,
                    limitIp,
                    totalGB = totalGb ?? 0,
                    expiryTime = expiry.ToUnixTimeMilliseconds(),
                    enable,
                    tgId = string.Empty,
                    subId = string.Empty,
                    reset = 0
                }
            }
        }, JsonOptions);

    private static string BuildClientEnabledSettings(string settingsJson, string clientId, bool enabled)
    {
        try
        {
            var root = JsonNode.Parse(string.IsNullOrWhiteSpace(settingsJson) ? "{}" : settingsJson) as JsonObject;
            var clients = root?["clients"] as JsonArray;
            var client = clients?
                .OfType<JsonObject>()
                .FirstOrDefault(x => string.Equals(x["id"]?.GetValue<string>(), clientId, StringComparison.Ordinal));
            if (client is null)
            {
                throw new InvalidOperationException("3x-ui inbound client configuration was not found.");
            }

            var updatedClient = client.DeepClone().AsObject();
            updatedClient["enable"] = enabled;
            return new JsonObject
            {
                ["clients"] = new JsonArray(updatedClient)
            }.ToJsonString(JsonOptions);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            throw new InvalidOperationException("3x-ui inbound client configuration is invalid.", ex);
        }
    }

    private static bool IsSuccessResponse(string raw, bool allowArrayResponse = false)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                return allowArrayResponse;
            }
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (root.TryGetProperty("success", out var success))
            {
                return success.ValueKind == JsonValueKind.True;
            }
            if (root.TryGetProperty("ok", out var ok))
            {
                return ok.ValueKind == JsonValueKind.True;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static IReadOnlyCollection<X3UiInboundDto> ParseInbounds(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
            var root = doc.RootElement;
            JsonElement array;
            if (root.ValueKind == JsonValueKind.Array)
            {
                array = root;
            }
            else if (root.ValueKind == JsonValueKind.Object
                     && root.TryGetProperty("obj", out var obj)
                     && obj.ValueKind == JsonValueKind.Array)
            {
                array = obj;
            }
            else if (root.ValueKind == JsonValueKind.Object
                     && root.TryGetProperty("data", out var data)
                     && data.ValueKind == JsonValueKind.Array)
            {
                array = data;
            }
            else
            {
                return Array.Empty<X3UiInboundDto>();
            }

            return array.EnumerateArray().Select(ReadInboundElement).ToList();
        }
        catch
        {
            return Array.Empty<X3UiInboundDto>();
        }
    }

    private static X3UiInboundDto? ParseInboundObject(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
            var root = doc.RootElement;
            var obj = root.TryGetProperty("obj", out var nested) && nested.ValueKind == JsonValueKind.Object ? nested : root;
            return ReadInboundElement(obj);
        }
        catch
        {
            return null;
        }
    }

    private static X3UiInboundDto ReadInboundElement(JsonElement item)
        => new(
            ReadFlexibleString(item, "id"),
            ReadString(item, "remark"),
            ReadString(item, "protocol"),
            ReadInt(item, "port"),
            ReadString(item, "listen"),
            ReadRawOrString(item, "settings"),
            ReadRawOrString(item, "streamSettings"),
            ReadRawOrString(item, "sniffing"),
            !item.TryGetProperty("enable", out var enable) || enable.ValueKind != JsonValueKind.False);

    private static string ReadRawOrString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value)) return "{}";
        return value.ValueKind == JsonValueKind.String ? value.GetString() ?? "{}" : value.GetRawText();
    }

    private static string ReadString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    private static string ReadFlexibleString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value)) return string.Empty;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.TryGetInt64(out var longValue) ? longValue.ToString(CultureInfo.InvariantCulture) : value.GetRawText(),
            _ => value.GetRawText()
        };
    }

    private static int ReadInt(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var parsed) ? parsed : 0;

    private static long TryReadLong(string raw, string propertyName)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
            var obj = doc.RootElement.TryGetProperty("obj", out var nested) ? nested : doc.RootElement;
            return obj.TryGetProperty(propertyName, out var value) && value.TryGetInt64(out var parsed) ? parsed : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static string TryReadString(string raw, string propertyName)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
            var root = doc.RootElement;
            return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string TryReadNestedString(string raw, params string[] path)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
            var current = doc.RootElement;
            foreach (var part in path)
            {
                if (!current.TryGetProperty(part, out current)) return string.Empty;
            }
            return current.ValueKind == JsonValueKind.String ? current.GetString() ?? string.Empty : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
