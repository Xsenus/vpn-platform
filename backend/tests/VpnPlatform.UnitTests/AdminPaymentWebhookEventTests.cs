using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AdminPaymentWebhookEventTests
{
    [Fact]
    public async Task GetPaymentWebhookEvents_Should_Query_Latest_200_And_Expose_Only_Safe_Operational_State()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options);
        await db.Database.EnsureCreatedAsync();

        var now = new DateTimeOffset(2026, 8, 12, 14, 0, 0, TimeSpan.Zero);
        for (var index = 0; index < 205; index++)
        {
            db.PaymentWebhookEvents.Add(Event(
                $"old-{index:D3}",
                PaymentWebhookEventStatus.Processed,
                now.AddHours(-1).AddMinutes(-index),
                "private-provider-exception"));
        }

        db.PaymentWebhookEvents.AddRange(
            Event("failed-latest", PaymentWebhookEventStatus.Failed, now, "private-provider-exception"),
            Event("rejected-latest", PaymentWebhookEventStatus.Rejected, now.AddMinutes(-1), "private-provider-exception"),
            Event("received-fresh", PaymentWebhookEventStatus.Received, now.AddMinutes(-2), "private-provider-exception"),
            Event("duplicate-latest", PaymentWebhookEventStatus.Duplicate, now.AddMinutes(-3), "private-provider-exception"),
            Event("received-boundary", PaymentWebhookEventStatus.Received, now.AddMinutes(-10), "private-provider-exception"),
            Event("verified-stale", PaymentWebhookEventStatus.Verified, now.AddMinutes(-11), "private-provider-exception"),
            Event("received-stale", PaymentWebhookEventStatus.Received, now.AddMinutes(-11), "private-provider-exception"));
        await db.SaveChangesAsync();

        var controller = new AdminOperationsController(db, null!, null!, null!, clock: new FixedClock(now));
        var ok = Assert.IsType<OkObjectResult>(await controller.GetPaymentWebhookEvents(CancellationToken.None));
        var response = JsonSerializer.Serialize(ok.Value);
        using var json = JsonDocument.Parse(response);
        var events = json.RootElement.EnumerateArray().ToList();

        Assert.Equal(200, events.Count);
        Assert.DoesNotContain("private-provider-exception", response, StringComparison.Ordinal);
        Assert.DoesNotContain("ErrorText", response, StringComparison.Ordinal);
        Assert.Equal("failed-latest", events[0].GetProperty("ExternalEventId").GetString());

        AssertFlags(events, "failed-latest", isRetryable: true, isTerminal: false, requiresAttention: true);
        AssertFlags(events, "rejected-latest", isRetryable: false, isTerminal: true, requiresAttention: true);
        AssertFlags(events, "received-fresh", isRetryable: false, isTerminal: false, requiresAttention: false);
        AssertFlags(events, "duplicate-latest", isRetryable: false, isTerminal: true, requiresAttention: false);
        AssertFlags(events, "received-boundary", isRetryable: true, isTerminal: false, requiresAttention: true);
        AssertFlags(events, "verified-stale", isRetryable: true, isTerminal: false, requiresAttention: true);
        AssertFlags(events, "received-stale", isRetryable: true, isTerminal: false, requiresAttention: true);
        Assert.DoesNotContain(events, item => item.GetProperty("ExternalEventId").GetString() == "old-204");
    }

    private static void AssertFlags(
        IReadOnlyCollection<JsonElement> events,
        string externalEventId,
        bool isRetryable,
        bool isTerminal,
        bool requiresAttention)
    {
        var item = Assert.Single(events, candidate => candidate.GetProperty("ExternalEventId").GetString() == externalEventId);
        Assert.Equal(isRetryable, item.GetProperty("IsRetryable").GetBoolean());
        Assert.Equal(isTerminal, item.GetProperty("IsTerminal").GetBoolean());
        Assert.Equal(requiresAttention, item.GetProperty("RequiresAttention").GetBoolean());
    }

    private static PaymentWebhookEvent Event(
        string externalEventId,
        PaymentWebhookEventStatus status,
        DateTimeOffset receivedAt,
        string errorText)
        => new()
        {
            Provider = PaymentProvider.YooKassa,
            ProviderPaymentId = $"payment-{externalEventId}",
            ExternalEventId = externalEventId,
            EventType = "payment.succeeded",
            PayloadSha256 = new string('a', 64),
            RawPayload = "{\"private\":\"payload\"}",
            HeadersJson = "{\"Authorization\":\"private\"}",
            SignatureValidated = status is PaymentWebhookEventStatus.Processed or PaymentWebhookEventStatus.Duplicate,
            Status = status,
            ReceivedAt = receivedAt,
            ProcessedAt = status is PaymentWebhookEventStatus.Received or PaymentWebhookEventStatus.Verified ? null : receivedAt,
            ErrorText = errorText
        };

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
