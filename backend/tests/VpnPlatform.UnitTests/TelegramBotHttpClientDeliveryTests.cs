using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Services;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Services;
using Xunit;

namespace VpnPlatform.UnitTests;

public class TelegramBotHttpClientDeliveryTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task Delivery_Methods_Should_Throw_When_Transport_Is_Unavailable(bool sendMessage, bool missingToken)
    {
        await using var db = CreateDbContext();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TelegramBot:Enabled"] = "true",
                ["TelegramBot:BotToken"] = missingToken ? string.Empty : "test-token"
            })
            .Build();
        var settings = new TelegramBotRuntimeSettingsService(db, configuration, new PassThroughSecretProtector());
        using var httpClient = new HttpClient(new StatusHandler(HttpStatusCode.BadGateway));
        var client = new TelegramBotHttpClient(httpClient, settings, NullLogger<TelegramBotHttpClient>.Instance);

        var exception = sendMessage
            ? await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendMessageAsync(777001, "hello", null, CancellationToken.None))
            : await Assert.ThrowsAsync<InvalidOperationException>(() => client.AnswerPreCheckoutQueryAsync("pre-1", true, null, CancellationToken.None));

        Assert.Contains(missingToken ? "BotToken" : "502", exception.Message, StringComparison.Ordinal);
    }

    private static ApplicationDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private sealed class StatusHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent("{}") });
    }

    private sealed class PassThroughSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue) => protectedValue;
        public string Mask(string? value, int visibleTail = 4) => value ?? string.Empty;
    }
}
