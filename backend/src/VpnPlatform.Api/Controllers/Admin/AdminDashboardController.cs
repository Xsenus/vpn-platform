using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Services;

namespace VpnPlatform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminRead)]
[Route("api/admin/dashboard")]
public class AdminDashboardController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IConfiguration? _configuration;
    private readonly IHostEnvironment? _environment;
    private readonly IClock _clock;

    public AdminDashboardController(
        IApplicationDbContext db,
        IConfiguration? configuration = null,
        IHostEnvironment? environment = null,
        IClock? clock = null)
    {
        _db = db;
        _configuration = configuration;
        _environment = environment;
        _clock = clock ?? new SystemClock();
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var expiringAt = now.AddDays(7);
        var recentSince = now.AddDays(-7);
        var roles = User?.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray() ?? [];
        var canReadFinance = AdminPolicies.HasAccess(roles, AdminPolicies.FinanceRead);
        var canReadSupport = AdminPolicies.HasAccess(roles, AdminPolicies.SupportRead);
        var canManageBot = AdminPolicies.HasAccess(roles, AdminPolicies.BotManage);
        var subscriptionCandidates = await _db.Subscriptions
            .AsNoTracking()
            .Where(x => x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.GracePeriod)
            .Select(x => new { x.Status, x.EndAt, x.GracePeriodEndAt })
            .ToListAsync(cancellationToken);
        var activeSubscriptionEndDates = subscriptionCandidates
            .Where(x => BusinessRules.IsSubscriptionAccessAvailable(x.Status, x.EndAt, x.GracePeriodEndAt, now))
            .Select(x => BusinessRules.GetSubscriptionAccessEnd(x.EndAt, x.GracePeriodEndAt))
            .ToList();
        var recentPaymentDates = canReadFinance
            ? await _db.Payments
            .AsNoTracking()
            .Select(x => x.CreatedAt)
            .ToListAsync(cancellationToken)
            : [];
        var recentOrderDates = canReadFinance
            ? await _db.Orders
            .AsNoTracking()
            .Select(x => x.CreatedAt)
            .ToListAsync(cancellationToken)
            : [];

        var summary = new AdminDashboardSummaryDto(
            await _db.Users.AsNoTracking().CountAsync(cancellationToken),
            await _db.TelegramAccounts.AsNoTracking().CountAsync(cancellationToken),
            activeSubscriptionEndDates.Count,
            activeSubscriptionEndDates.Count(x => x <= expiringAt),
            canReadFinance ? await _db.Orders.AsNoTracking().CountAsync(x => x.Status == OrderStatus.PaymentReceived || x.Status == OrderStatus.Completed, cancellationToken) : 0,
            canReadFinance ? await _db.Orders.AsNoTracking().CountAsync(x => x.Status == OrderStatus.PendingPayment || x.Status == OrderStatus.Draft, cancellationToken) : 0,
            canReadFinance ? await _db.Payments.AsNoTracking().CountAsync(x => x.Status == PaymentStatus.Failed || x.Status == PaymentStatus.Cancelled, cancellationToken) : 0,
            recentPaymentDates.Count(x => x >= recentSince),
            recentOrderDates.Count(x => x >= recentSince),
            await _db.AccessCredentials.AsNoTracking().CountAsync(cancellationToken),
            await _db.VpnNodes.AsNoTracking().CountAsync(cancellationToken),
            await _db.VpnNodes.AsNoTracking().CountAsync(x => x.HealthStatus == HealthStatus.Healthy, cancellationToken),
            await _db.VpnPanels.AsNoTracking().CountAsync(cancellationToken),
            await _db.VpnPanels.AsNoTracking().CountAsync(x => x.HealthStatus == HealthStatus.Healthy, cancellationToken),
            canReadSupport ? await _db.SupportConversations.AsNoTracking().CountAsync(cancellationToken) : 0,
            canReadSupport ? await _db.SupportConversations.AsNoTracking().CountAsync(x => x.Status == "open" || x.Status == "pending", cancellationToken) : 0,
            await _db.ProvisioningRuns.AsNoTracking().CountAsync(x => x.Status == ProvisioningRunStatus.Failed, cancellationToken),
            await BuildProductionReadinessAsync(canReadFinance, canManageBot, cancellationToken),
            now);

        return Ok(summary);
    }

    private async Task<AdminProductionReadinessDto> BuildProductionReadinessAsync(
        bool canReadFinance,
        bool canManageBot,
        CancellationToken cancellationToken)
    {
        var productionPaymentAccounts = canReadFinance
            ? await _db.PaymentProviderAccounts
            .AsNoTracking()
            .Where(x => x.IsEnabled && x.Mode == PaymentProviderMode.Production && x.Provider != PaymentProvider.TelegramStars)
            .ToListAsync(cancellationToken)
            : [];
        var livePaymentProviders = productionPaymentAccounts.Count(x =>
            x.SecretKeyProtected != string.Empty &&
            (x.ShopId != string.Empty || x.Provider == PaymentProvider.Stripe || x.Provider == PaymentProvider.PayPal));
        var livePaymentWebhooks = productionPaymentAccounts.Count(x =>
            x.SecretKeyProtected != string.Empty &&
            !string.IsNullOrWhiteSpace(x.WebhookUrl) &&
            (x.ShopId != string.Empty || x.Provider == PaymentProvider.Stripe || x.Provider == PaymentProvider.PayPal));

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

        var telegramState = canManageBot
            ? await BuildTelegramReadinessStateAsync(cancellationToken)
            : (IsReady: false, Message: string.Empty);
        var failedProvisioningRuns = await _db.ProvisioningRuns
            .AsNoTracking()
            .CountAsync(x => x.Status == ProvisioningRunStatus.Failed || x.Status == ProvisioningRunStatus.PrecheckFailed, cancellationToken);
        var ciCdReady = HasWorkflowFile("ci.yml") && HasWorkflowFile("deploy-vps.yml");

        var checks = new[]
        {
            Check("payment-provider", "Live-провайдер оплаты", livePaymentProviders > 0, livePaymentProviders > 0 ? $"Готово: {livePaymentProviders} production-аккаунт(ов)." : "Нет включенного production-провайдера с обязательными секретами.", "Платежи", "critical", "Открыть платежи", "#payments"),
            Check("payment-webhook", "Webhook платежей", livePaymentProviders > 0 && livePaymentWebhooks > 0, livePaymentWebhooks > 0 ? $"Готово: webhook URL заполнен у {livePaymentWebhooks} production-аккаунт(ов)." : "Нет production-провайдера с заполненным webhook URL для статусов оплаты.", "Платежи", "critical", "Открыть платежи", "#payments"),
            Check("paid-tariff", "Активный платный тариф", activePaidTariffs > 0, activePaidTariffs > 0 ? $"Готово: {activePaidTariffs} платный тариф(ов)." : "Нет активного платного тарифа для продажи.", "Тарифы", "critical", "Открыть тарифы", "#tariffs"),
            Check("vpn-panel", "Реальная 3x-ui панель", realPanels > 0, realPanels > 0 ? $"Готово: {realPanels} активная панель(и)." : "Нет активной реальной 3x-ui панели. Sandbox-панель не считается.", "VPN", "critical", "Открыть панели", "#panels"),
            Check("vpn-inbound", "Активный inbound", realInbounds > 0, realInbounds > 0 ? $"Готово: {realInbounds} inbound(ов)." : "Нет активного inbound на реальной панели.", "VPN", "critical", "Открыть панели", "#panels"),
            Check("vpn-node", "Реальный VPN-сервер", realNodes > 0, realNodes > 0 ? $"Готово: {realNodes} сервер(ов) открыт(ы) для выдачи." : "Нет готового реального VPN-сервера. Sandbox-нода не считается.", "VPN", "critical", "Открыть серверы", "#nodes"),
            Check("telegram-bot", "Telegram-бот", telegramState.IsReady, telegramState.Message, "Telegram", "warning", "Открыть Telegram", "#bot"),
            Check("vps-provisioning", "Очередь VPS provisioning", failedProvisioningRuns == 0, failedProvisioningRuns == 0 ? "Готово: нет упавших precheck/deploy запусков." : $"Есть упавшие provisioning-запуски: {failedProvisioningRuns}.", "VPS", "warning", "Открыть VPS", "#provisioning"),
            Check("ci-cd", "CI/CD workflow", ciCdReady, ciCdReady ? "Готово: найдены workflow для CI и VPS deploy." : "Не найдены workflow ci.yml и deploy-vps.yml в .github/workflows.", "CI/CD", "warning", "Открыть деплой", "#provisioning")
        }
        .Where(check => canReadFinance || check.Key is not ("payment-provider" or "payment-webhook"))
        .Where(check => canManageBot || check.Key != "telegram-bot")
        .ToArray();

        var isReady = checks.All(x => x.Status == "Ready");
        return new AdminProductionReadinessDto(isReady, isReady ? "Ready" : "Blocked", checks);
    }

    private async Task<(bool IsReady, string Message)> BuildTelegramReadinessStateAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.SiteContentBlocks
            .AsNoTracking()
            .Where(x => x.Group == "telegram_bot" && x.IsActive)
            .Select(x => new { x.Key, x.Value })
            .ToListAsync(cancellationToken);
        var values = settings
            .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last().Value, StringComparer.OrdinalIgnoreCase);

        var enabled = ReadBool(values, "telegram_bot.enabled", _configuration?.GetValue<bool>("TelegramBot:Enabled") ?? false);
        var mode = Read(values, "telegram_bot.mode", _configuration?["TelegramBot:Mode"] ?? "LongPolling");
        var username = Read(values, "telegram_bot.public_bot_username", _configuration?["TelegramBot:PublicBotUsername"] ?? string.Empty);
        var token = Read(values, "telegram_bot.bot_token_protected", _configuration?["TelegramBot:BotToken"] ?? string.Empty);
        var webhookUrl = Read(values, "telegram_bot.webhook_url", _configuration?["TelegramBot:WebhookUrl"] ?? string.Empty);

        if (!enabled)
        {
            return (false, "Telegram-бот выключен. Пользователи не смогут покупать и получать уведомления через Telegram.");
        }

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(token))
        {
            return (false, "Не заполнены public username или Bot token Telegram-бота.");
        }

        if (mode.Equals("Webhook", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(webhookUrl))
        {
            return (false, "Для режима Webhook нужен публичный Telegram webhook URL.");
        }

        return (true, mode.Equals("Webhook", StringComparison.OrdinalIgnoreCase)
            ? "Готово: Telegram-бот включен, token сохранен, webhook URL заполнен."
            : "Готово: Telegram-бот включен, token сохранен, LongPolling доступен.");
    }

    private bool HasWorkflowFile(string fileName)
    {
        var roots = new[]
        {
            _environment?.ContentRootPath,
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var root in roots)
        {
            var directory = new DirectoryInfo(root!);
            for (var depth = 0; directory is not null && depth < 8; depth++, directory = directory.Parent)
            {
                var path = Path.Combine(directory.FullName, ".github", "workflows", fileName);
                if (System.IO.File.Exists(path))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string Read(IReadOnlyDictionary<string, string> values, string key, string fallback)
        => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

    private static bool ReadBool(IReadOnlyDictionary<string, string> values, string key, bool fallback)
        => values.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) ? parsed : fallback;

    private static AdminProductionReadinessCheckDto Check(string key, string label, bool ready, string message, string category, string severity, string actionLabel, string actionHref)
        => new(key, label, ready ? "Ready" : "Blocked", message, category, severity, actionLabel, actionHref);

}
