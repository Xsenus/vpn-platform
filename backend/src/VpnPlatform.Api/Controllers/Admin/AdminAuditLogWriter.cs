using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Entities;

namespace VpnPlatform.Api.Controllers.Admin;

internal static class AdminAuditLogWriter
{
    public static void Add(
        IApplicationDbContext db,
        ControllerBase controller,
        string action,
        string entityType,
        Guid entityId,
        object? before,
        object? after)
    {
        var httpContext = controller.ControllerContext.HttpContext;
        var user = httpContext?.User;
        var actorId = user?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user?.FindFirstValue("sub")
            ?? user?.Identity?.Name
            ?? "unknown";

        db.AuditLogs.Add(new AuditLog
        {
            ActorType = "admin",
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            BeforeJson = SensitiveDataRedactor.Redact(Serialize(before)),
            AfterJson = SensitiveDataRedactor.Redact(Serialize(after)),
            Ip = httpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            UserAgent = httpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty
        });
    }

    private static string Serialize(object? snapshot)
        => snapshot switch
        {
            null => "{}",
            string json => json,
            _ => JsonSerializer.Serialize(snapshot)
        };
}
