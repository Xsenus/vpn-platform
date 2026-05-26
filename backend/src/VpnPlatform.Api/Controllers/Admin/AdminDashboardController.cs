using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminRead)]
[Route("api/admin/dashboard")]
public class AdminDashboardController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public AdminDashboardController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var expiringAt = now.AddDays(7);
        var recentSince = now.AddDays(-7);
        var activeSubscriptionEndDates = await _db.Subscriptions
            .AsNoTracking()
            .Where(x => x.Status == SubscriptionStatus.Active)
            .Select(x => x.EndAt)
            .ToListAsync(cancellationToken);
        var recentPaymentDates = await _db.Payments
            .AsNoTracking()
            .Select(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var recentOrderDates = await _db.Orders
            .AsNoTracking()
            .Select(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var summary = new AdminDashboardSummaryDto(
            await _db.Users.AsNoTracking().CountAsync(cancellationToken),
            await _db.TelegramAccounts.AsNoTracking().CountAsync(cancellationToken),
            activeSubscriptionEndDates.Count,
            activeSubscriptionEndDates.Count(x => x <= expiringAt),
            await _db.Orders.AsNoTracking().CountAsync(x => x.Status == OrderStatus.PaymentReceived || x.Status == OrderStatus.Completed, cancellationToken),
            await _db.Orders.AsNoTracking().CountAsync(x => x.Status == OrderStatus.PendingPayment || x.Status == OrderStatus.Draft, cancellationToken),
            await _db.Payments.AsNoTracking().CountAsync(x => x.Status == PaymentStatus.Failed || x.Status == PaymentStatus.Cancelled, cancellationToken),
            recentPaymentDates.Count(x => x >= recentSince),
            recentOrderDates.Count(x => x >= recentSince),
            await _db.AccessCredentials.AsNoTracking().CountAsync(cancellationToken),
            await _db.VpnNodes.AsNoTracking().CountAsync(cancellationToken),
            await _db.VpnNodes.AsNoTracking().CountAsync(x => x.HealthStatus == HealthStatus.Healthy, cancellationToken),
            await _db.VpnPanels.AsNoTracking().CountAsync(cancellationToken),
            await _db.VpnPanels.AsNoTracking().CountAsync(x => x.HealthStatus == HealthStatus.Healthy, cancellationToken),
            await _db.SupportConversations.AsNoTracking().CountAsync(cancellationToken),
            await _db.SupportConversations.AsNoTracking().CountAsync(x => x.Status == "open" || x.Status == "pending", cancellationToken),
            await _db.ProvisioningRuns.AsNoTracking().CountAsync(x => x.Status == ProvisioningRunStatus.Failed, cancellationToken),
            now);

        return Ok(summary);
    }
}
