using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.BotManage)]
[Route("api/admin/telegram-bot")]
public class AdminTelegramBotSettingsController : ControllerBase
{
    private const string WelcomeKey = "telegram.welcome";
    private const string InstructionKey = "telegram.instruction";
    private const string SupportKey = "telegram.support";
    private const string AfterPaymentKey = "telegram.after_payment";
    private const string RenewalKey = "telegram.renewal";
    private const string PaymentFailedKey = "telegram.payment_failed";
    private const string SubscriptionExpiredKey = "telegram.subscription_expired";
    private const string SettingsGroup = "telegram_bot";
    private const string EnabledKey = "telegram_bot.enabled";
    private const string ModeKey = "telegram_bot.mode";
    private const string PublicBotUsernameKey = "telegram_bot.public_bot_username";
    private const string BotTokenProtectedKey = "telegram_bot.bot_token_protected";
    private const string WebhookUrlKey = "telegram_bot.webhook_url";
    private const string SecretTokenProtectedKey = "telegram_bot.secret_token_protected";
    private const string AdminChatIdKey = "telegram_bot.admin_chat_id";
    private const string WebAppUrlKey = "telegram_bot.web_app_url";

    private readonly IApplicationDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ISecretProtector _secretProtector;

    public AdminTelegramBotSettingsController(IApplicationDbContext db, IConfiguration configuration, ISecretProtector secretProtector)
    {
        _db = db;
        _configuration = configuration;
        _secretProtector = secretProtector;
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var templates = await _db.NotificationTemplates
            .AsNoTracking()
            .Where(x => x.Channel == NotificationChannelType.Telegram && x.Language == "ru")
            .ToListAsync(cancellationToken);

        var settings = await _db.SiteContentBlocks
            .AsNoTracking()
            .Where(x => x.Group == SettingsGroup)
            .ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);

        var token = ReadProtectedSetting(settings, BotTokenProtectedKey) ?? _configuration["TelegramBot:BotToken"] ?? string.Empty;
        var secretToken = ReadProtectedSetting(settings, SecretTokenProtectedKey) ?? _configuration["TelegramBot:SecretToken"] ?? string.Empty;

        return Ok(new AdminTelegramBotSettingsDto(
            ReadBoolSetting(settings, EnabledKey, _configuration.GetValue<bool>("TelegramBot:Enabled")),
            NormalizeMode(ReadSetting(settings, ModeKey, _configuration["TelegramBot:Mode"] ?? "LongPolling")) ?? "LongPolling",
            ReadSetting(settings, PublicBotUsernameKey, _configuration["TelegramBot:PublicBotUsername"] ?? string.Empty),
            !string.IsNullOrWhiteSpace(token),
            MaskToken(token),
            ReadSetting(settings, WebhookUrlKey, _configuration["TelegramBot:WebhookUrl"] ?? string.Empty),
            !string.IsNullOrWhiteSpace(secretToken),
            ReadSetting(settings, AdminChatIdKey, _configuration["TelegramBot:AdminChatId"] ?? string.Empty),
            ReadSetting(settings, WebAppUrlKey, _configuration["TelegramBot:WebAppUrl"] ?? string.Empty),
            FindTemplate(templates, WelcomeKey, "Добро пожаловать! Выберите действие в меню."),
            FindTemplate(templates, InstructionKey, "Инструкция появится после выдачи VPN-доступа."),
            FindTemplate(templates, SupportKey, "Опишите проблему одним сообщением, оператор ответит в Telegram."),
            FindTemplate(templates, AfterPaymentKey, "Оплата получена. Ваш VPN-доступ готов."),
            FindTemplate(templates, RenewalKey, "Продление оформлено. После оплаты подписка будет продлена автоматически."),
            FindTemplate(templates, PaymentFailedKey, "Оплата не прошла. Проверьте способ оплаты или попробуйте другой вариант."),
            FindTemplate(templates, SubscriptionExpiredKey, "Срок подписки истек. Продлите тариф, чтобы восстановить VPN-доступ."),
            DateTimeOffset.UtcNow));
    }

    [HttpPatch("settings")]
    [Authorize(Policy = AdminPolicies.BotManage)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateTelegramBotSettingsCommand request, CancellationToken cancellationToken)
    {
        await UpsertSettingAsync(EnabledKey, "Включен", request.Enabled?.ToString().ToLowerInvariant(), "checkbox", cancellationToken);
        await UpsertSettingAsync(ModeKey, "Режим", NormalizeMode(request.Mode), "select", cancellationToken);
        await UpsertSettingAsync(PublicBotUsernameKey, "Public bot username", NormalizeUsername(request.PublicBotUsername), "text", cancellationToken);
        await UpsertSettingAsync(WebhookUrlKey, "Webhook URL", request.WebhookUrl?.Trim(), "url", cancellationToken);
        await UpsertSettingAsync(AdminChatIdKey, "Admin chat id", request.AdminChatId?.Trim(), "text", cancellationToken);
        await UpsertSettingAsync(WebAppUrlKey, "WebApp URL", request.WebAppUrl?.Trim(), "url", cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.BotToken))
        {
            await UpsertSettingAsync(BotTokenProtectedKey, "Bot token", _secretProtector.Protect(request.BotToken.Trim()), "secret", cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.SecretToken))
        {
            await UpsertSettingAsync(SecretTokenProtectedKey, "Secret token", _secretProtector.Protect(request.SecretToken.Trim()), "secret", cancellationToken);
        }

        await UpsertTemplateAsync(WelcomeKey, "Welcome text", request.WelcomeText, cancellationToken);
        await UpsertTemplateAsync(InstructionKey, "Instruction text", request.InstructionText, cancellationToken);
        await UpsertTemplateAsync(SupportKey, "Support text", request.SupportText, cancellationToken);
        await UpsertTemplateAsync(AfterPaymentKey, "After payment text", request.AfterPaymentTextTemplate, cancellationToken);
        await UpsertTemplateAsync(RenewalKey, "Renewal text", request.RenewalTextTemplate, cancellationToken);
        await UpsertTemplateAsync(PaymentFailedKey, "Payment failed text", request.PaymentFailedTextTemplate, cancellationToken);
        await UpsertTemplateAsync(SubscriptionExpiredKey, "Subscription expired text", request.SubscriptionExpiredTextTemplate, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetSettings(cancellationToken);
    }

    private async Task UpsertSettingAsync(string key, string label, string? value, string inputType, CancellationToken cancellationToken)
    {
        if (value is null)
        {
            return;
        }

        var setting = await _db.SiteContentBlocks.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (setting is null)
        {
            _db.SiteContentBlocks.Add(new SiteContentBlock
            {
                Key = key,
                Group = SettingsGroup,
                Label = label,
                Value = value,
                InputType = inputType,
                IsActive = true
            });
            return;
        }

        setting.Group = SettingsGroup;
        setting.Label = label;
        setting.Value = value;
        setting.InputType = inputType;
        setting.IsActive = true;
        setting.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private async Task UpsertTemplateAsync(string key, string subject, string? body, CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return;
        }

        var normalized = body.Trim();
        var template = await _db.NotificationTemplates
            .FirstOrDefaultAsync(x => x.Key == key && x.Channel == NotificationChannelType.Telegram && x.Language == "ru", cancellationToken);

        if (template is null)
        {
            _db.NotificationTemplates.Add(new NotificationTemplate
            {
                Key = key,
                Channel = NotificationChannelType.Telegram,
                Language = "ru",
                Subject = subject,
                Body = normalized,
                IsActive = true
            });
            return;
        }

        template.Subject = subject;
        template.Body = normalized;
        template.IsActive = true;
        template.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string FindTemplate(IEnumerable<NotificationTemplate> templates, string key, string fallback)
        => templates.FirstOrDefault(x => x.Key == key && x.IsActive)?.Body ?? fallback;

    private string? ReadProtectedSetting(IReadOnlyDictionary<string, string> settings, string key)
    {
        if (!settings.TryGetValue(key, out var protectedValue) || string.IsNullOrWhiteSpace(protectedValue))
        {
            return null;
        }

        try
        {
            return _secretProtector.Unprotect(protectedValue);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ReadSetting(IReadOnlyDictionary<string, string> settings, string key, string fallback)
        => settings.TryGetValue(key, out var value) ? value : fallback;

    private static bool ReadBoolSetting(IReadOnlyDictionary<string, string> settings, string key, bool fallback)
        => settings.TryGetValue(key, out var value) ? bool.TryParse(value, out var parsed) && parsed : fallback;

    private static string? NormalizeMode(string? mode)
    {
        if (mode is null)
        {
            return null;
        }

        var normalized = mode.Trim();
        return string.Equals(normalized, "Webhook", StringComparison.OrdinalIgnoreCase) ? "Webhook" : "LongPolling";
    }

    private static string? NormalizeUsername(string? username)
        => username is null ? null : username.Trim().TrimStart('@');

    private static string MaskToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        var trimmed = token.Trim();
        if (trimmed.Length <= 8)
        {
            return "***";
        }

        return $"{trimmed[..4]}***{trimmed[^4..]}";
    }
}
