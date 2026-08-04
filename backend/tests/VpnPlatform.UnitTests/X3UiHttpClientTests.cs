using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Vpn;
using VpnPlatform.Application.DTOs;
using Xunit;

namespace VpnPlatform.UnitTests;

public class X3UiHttpClientTests
{
    [Fact]
    public async Task Login_Should_Return_Session_Cookie()
    {
        var handler = new QueueHandler(LoginResponse());
        var client = CreateClient(handler);

        var session = await client.LoginAsync(Panel(), "secret", CancellationToken.None);

        Assert.Equal("session=test", session.SessionCookie);
        Assert.Contains(handler.Requests, x => x.Path == "/login" && x.Body.Contains("admin") && x.Body.Contains("secret"));
    }

    [Fact]
    public async Task Login_Failure_Should_Throw()
    {
        var handler = new QueueHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("{\"success\":false}") });
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.LoginAsync(Panel(), "secret", CancellationToken.None));
    }

    [Fact]
    public async Task Health_Check_Should_Propagate_Caller_Cancellation()
    {
        var handler = new QueueHandler(LoginResponse());
        var client = CreateClient(handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.CheckHealthAsync(Panel(), "secret", cancellation.Token));
    }

    [Fact]
    public async Task GetInbounds_Should_Login_And_Parse_Common_Response_Shape()
    {
        var handler = new QueueHandler(
            LoginResponse(),
            JsonResponse("{\"success\":true,\"obj\":[{\"id\":1,\"remark\":\"vless\",\"protocol\":\"vless\",\"port\":443,\"listen\":\"\",\"settings\":{\"clients\":[]},\"streamSettings\":{\"network\":\"tcp\",\"security\":\"tls\"},\"sniffing\":{},\"enable\":true}]}")
        );
        var client = CreateClient(handler);

        var inbounds = await client.GetInboundsAsync(Panel(), "secret", CancellationToken.None);

        var inbound = Assert.Single(inbounds);
        Assert.Equal("1", inbound.Id);
        Assert.Equal("vless", inbound.Protocol);
        Assert.Contains(handler.Requests, x => x.Path == "/panel/api/inbounds/list" && x.Cookie == "session=test");
    }

    [Fact]
    public async Task AddClient_Should_Treat_Empty_3xUi_Response_As_Success()
    {
        var handler = new QueueHandler(LoginResponse(), new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) });
        var client = CreateClient(handler);

        var created = await client.AddClientAsync(Panel(), "secret", new X3UiAddClientRequest("1", "user@example.test", "11111111-1111-1111-1111-111111111111", string.Empty, 3, null, DateTimeOffset.UtcNow.AddDays(30), true), CancellationToken.None);

        Assert.Equal("user@example.test", created.Email);
        Assert.Contains(handler.Requests, x => x.Path == "/panel/api/inbounds/addClient" && x.Body.Contains("11111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public async Task DeleteInbound_Should_Call_Delete_Endpoint()
    {
        var handler = new QueueHandler(LoginResponse(), new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) });
        var client = CreateClient(handler);

        await client.DeleteInboundAsync(Panel(), "secret", "inbound/1", CancellationToken.None);

        Assert.Contains(handler.Requests, x => x.Path == "/panel/api/inbounds/del/inbound%2F1");
    }

    [Fact]
    public async Task UpdateClient_Should_Send_Expiry_Request()
    {
        var handler = new QueueHandler(LoginResponse(), new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) });
        var client = CreateClient(handler);
        var expiry = new DateTimeOffset(2026, 5, 30, 0, 0, 0, TimeSpan.Zero);

        await client.UpdateClientAsync(Panel(), "secret", new X3UiUpdateClientRequest("1", "client-1", "user@example.test", "11111111-1111-1111-1111-111111111111", "xtls-rprx-vision", 2, 1024, expiry, true), CancellationToken.None);

        var request = Assert.Single(handler.Requests, x => x.Path == "/panel/api/inbounds/updateClient/client-1");
        Assert.Contains("11111111-1111-1111-1111-111111111111", request.Body);
        Assert.Contains(expiry.ToUnixTimeMilliseconds().ToString(), request.Body);
    }

    [Fact]
    public async Task DisableClient_Should_Use_UpdateClient_Endpoint()
    {
        var handler = new QueueHandler(
            LoginResponse(),
            JsonResponse("{\"success\":true,\"obj\":{\"up\":1,\"down\":2}}"),
            LoginResponse(),
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) });
        var client = CreateClient(handler);

        await client.DisableClientAsync(Panel(), "secret", "1", "client-1", CancellationToken.None);

        var update = Assert.Single(handler.Requests, x => x.Path == "/panel/api/inbounds/updateClient/client-1");
        Assert.Contains("\\\"enable\\\":false", update.Body);
    }

    [Fact]
    public async Task DeleteClient_Should_Call_Delete_Endpoint()
    {
        var handler = new QueueHandler(LoginResponse(), new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) });
        var client = CreateClient(handler);

        await client.DeleteClientAsync(Panel(), "secret", "1", "client-1", CancellationToken.None);

        Assert.Contains(handler.Requests, x => x.Path == "/panel/api/inbounds/delClient/1/client-1");
    }

    [Fact]
    public async Task Error_Response_Should_Throw_Without_Leaking_Secret_In_Exception()
    {
        var handler = new QueueHandler(LoginResponse(), new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("panel password secret session=test") });
        var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.DeleteClientAsync(Panel(), "secret", "1", "client-1", CancellationToken.None));

        Assert.DoesNotContain("secret", ex.Message.ToLowerInvariant());
        Assert.DoesNotContain("session=test", ex.Message.ToLowerInvariant());
    }

    private static X3UiHttpClient CreateClient(QueueHandler handler)
        => new(new FakeHttpClientFactory(new HttpClient(handler)), NullLogger<X3UiHttpClient>.Instance);

    private static VpnPanel Panel()
        => new() { Id = Guid.NewGuid(), Name = "panel", BaseUrl = "https://panel.test", Login = "admin", SslVerificationMode = VpnSslVerificationMode.Strict };

    private static HttpResponseMessage LoginResponse()
    {
        var response = JsonResponse("{\"success\":true}");
        response.Headers.Add("Set-Cookie", "session=test; Path=/; HttpOnly");
        return response;
    }

    private static HttpResponseMessage JsonResponse(string json)
        => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public FakeHttpClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class QueueHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;
        public List<CapturedRequest> Requests { get; } = new();

        public QueueHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new CapturedRequest(request.RequestUri?.AbsolutePath ?? string.Empty, body, request.Headers.TryGetValues("Cookie", out var cookies) ? string.Join(";", cookies) : string.Empty));
            return _responses.Count > 0 ? _responses.Dequeue() : new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{}") };
        }
    }

    private sealed record CapturedRequest(string Path, string Body, string Cookie);
}
