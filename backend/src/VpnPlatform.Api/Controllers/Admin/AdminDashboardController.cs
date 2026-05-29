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
            await BuildProductionReadinessAsync(cancellationToken),
            now);

        return Ok(summary);
    }

    private async Task<AdminProductionReadinessDto> BuildProductionReadinessAsync(CancellationToken cancellationToken)
    {
        var livePaymentProviders = await _db.PaymentProviderAccounts
            .AsNoTracking()
            .CountAsync(x =>
                x.IsEnabled &&
                x.Mode == PaymentProviderMode.Production &&
                x.Provider != PaymentProvider.TelegramStars &&
                x.SecretKeyProtected != string.Empty &&
                (x.ShopId != string.Empty || x.Provider == PaymentProvider.Stripe || x.Provider == PaymentProvider.PayPal),
                cancellationToken);

        var activePaidTariffs = await _db.Tariffs
            .AsNoTracking()
            .CountAsync(x => x.IsActive && !x.IsTrial && x.Price > 0, cancellationToken);

        var realPanels = await _db.VpnPanels
            .AsNoTracking()
            .CountAsync(x =>
                x.Status == VpnPanelStatus.Active &&
                x.HealthStatus != HealthStatus.Unhealthy &&
                x.UsedCapacity < x.Capacity &&
                x.Region != "sandbox" &&
                x.Name != "sandbox-x3ui-panel" &&
                x.BaseUrl != "https://sandbox-node.local",
                cancellationToken);

        var realInbounds = await _db.VpnInbounds
            .AsNoTracking()
            .Include(x => x.VpnPanel)
            .CountAsync(x =>
                x.IsActive &&
                x.UsedCapacity < x.Capacity &&
                x.VpnPanel != null &&
                x.VpnPanel.Status == VpnPanelStatus.Active &&
                x.VpnPanel.HealthStatus != HealthStatus.Unhealthy &&
                x.VpnPanel.Region != "sandbox" &&
                x.VpnPanel.Name != "sandbox-x3ui-panel" &&
                x.VpnPanel.BaseUrl != "https://sandbox-node.local",
                cancellationToken);

        var realNodes = await _db.VpnNodes
            .AsNoTracking()
            .CountAsync(x =>
                x.Status == NodeStatus.Ready &&
                x.IsAvailableForNewUsers &&
                x.HealthStatus != HealthStatus.Unhealthy &&
                x.UsedCapacity < x.Capacity &&
                x.Region != "sandbox" &&
                x.Name != "sandbox-vpn-node" &&
                x.PanelBaseUrl != "https://sandbox-node.local" &&
                !x.TagsCsv.ToLower().Contains("sandbox"),
                cancellationToken);

        var checks = new[]
        {
            Check("payment-provider", "Live-провайдер оплаты", livePaymentProviders > 0, livePaymentProviders > 0 ? $"Готово: {livePaymentProviders} production-аккаунт(ов)." : "Нет включенного production-провайдера с обязательными секретами."),
            Check("paid-tariff", "Активный платный тариф", activePaidTariffs > 0, activePaidTariffs > 0 ? $"Готово: {activePaidTariffs} платный тариф(ов)." : "Нет активного платного тарифа для продажи."),
            Check("vpn-panel", "Реальная 3x-ui панель", realPanels > 0, realPanels > 0 ? $"Готово: {realPanels} активная панель(и)." : "Нет активной реальной 3x-ui панели. Sandbox-панель не считается."),
            Check("vpn-inbound", "Активный inbound", realInbounds > 0, realInbounds > 0 ? $"Готово: {realInbounds} inbound(ов)." : "Нет активного inbound на реальной панели."),
            Check("vpn-node", "Реальный VPN-сервер", realNodes > 0, realNodes > 0 ? $"Готово: {realNodes} сервер(ов) открыт(ы) для выдачи." : "Нет готового реального VPN-сервера. Sandbox-нода не считается.")
        };

        var isReady = checks.All(x => x.Status == "Ready");
        return new AdminProductionReadinessDto(isReady, isReady ? "Ready" : "Blocked", checks);
    }

    private static AdminProductionReadinessCheckDto Check(string key, string label, bool ready, string message)
        => new(key, label, ready ? "Ready" : "Blocked", message);

}
