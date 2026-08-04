using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;

namespace VpnPlatform.Infrastructure.Services;

public sealed class LocalOutboxMessageSink : IOutboxMessageSink
{
    private readonly ApplicationDbContext _db;

    public LocalOutboxMessageSink(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task DispatchAsync(
        Guid messageId,
        string type,
        string correlationId,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        using var payload = ParsePayload(payloadJson);
        switch (type)
        {
            case "NotificationRequested":
                await MaterializeNotificationAsync(messageId, payload.RootElement, payloadJson, cancellationToken);
                return;
            case "password_reset_requested":
                MaterializePasswordReset(messageId, payload.RootElement, payloadJson);
                return;
            case "PaymentStatusChanged":
                RequireGuid(payload.RootElement, "paymentId");
                RequireGuid(payload.RootElement, "orderId");
                RequireString(payload.RootElement, "status");
                return;
            case "OrderTimelineEvent":
                RequireGuid(payload.RootElement, "orderId");
                RequireString(payload.RootElement, "eventType");
                return;
            default:
                throw new OutboxPermanentDispatchException($"Unsupported outbox message type '{type}'.");
        }
    }

    private async Task MaterializeNotificationAsync(
        Guid messageId,
        JsonElement payload,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var userId = RequireGuid(payload, "userId");
        var templateKey = RequireString(payload, "templateKey");
        var email = await _db.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.Email)
            .SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new OutboxPermanentDispatchException("Notification recipient email is unavailable.");
        }

        _db.NotificationDeliveries.Add(new NotificationDelivery
        {
            UserId = userId,
            TemplateKey = templateKey,
            Channel = NotificationChannelType.Email,
            ToAddress = email,
            Status = NotificationDeliveryStatus.Pending,
            PayloadJson = AddSourceMessageId(payloadJson, messageId)
        });
    }

    private void MaterializePasswordReset(Guid messageId, JsonElement payload, string payloadJson)
    {
        var userId = RequireGuid(payload, "userId");
        var email = RequireString(payload, "email");
        _db.NotificationDeliveries.Add(new NotificationDelivery
        {
            UserId = userId,
            TemplateKey = "password_reset_requested",
            Channel = NotificationChannelType.Email,
            ToAddress = email,
            Status = NotificationDeliveryStatus.Pending,
            PayloadJson = AddSourceMessageId(payloadJson, messageId)
        });
    }

    private static JsonDocument ParsePayload(string payloadJson)
    {
        try
        {
            var document = JsonDocument.Parse(payloadJson);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                return document;
            }

            document.Dispose();
        }
        catch (JsonException)
        {
        }

        throw new OutboxPermanentDispatchException("Outbox payload must be a valid JSON object.");
    }

    private static string RequireString(JsonElement payload, string propertyName)
    {
        if (!payload.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new OutboxPermanentDispatchException($"Outbox payload property '{propertyName}' is required.");
        }

        return value.GetString()!;
    }

    private static Guid RequireGuid(JsonElement payload, string propertyName)
    {
        var value = RequireString(payload, propertyName);
        if (!Guid.TryParse(value, out var parsed))
        {
            throw new OutboxPermanentDispatchException($"Outbox payload property '{propertyName}' must be a GUID.");
        }

        return parsed;
    }

    private static string AddSourceMessageId(string payloadJson, Guid messageId)
    {
        using var document = JsonDocument.Parse(payloadJson);
        var properties = document.RootElement.EnumerateObject()
            .ToDictionary(x => x.Name, x => x.Value.Clone(), StringComparer.Ordinal);
        properties["sourceOutboxMessageId"] = JsonSerializer.SerializeToElement(messageId);
        return JsonSerializer.Serialize(properties);
    }
}
