using System.Security.Claims;
using System.Text.Json;
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
        var state = await LoadStateAsync(cancellationToken);
        return Ok(ToDto(state));
    }

    [HttpPost("settings/test")]
    [Authorize(Policy = AdminPolicies.BotManage)]
    public async Task<IActionResult> TestSettings(CancellationToken cancellationToken)
    {
        var state = await LoadStateAsync(cancellationToken);
        return Ok(BuildConnectionCheck(state));
    }

    [HttpPatch("settings")]
    [Authorize(Policy = AdminPolicies.BotManage)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateTelegramBotSettingsCommand request, CancellationToken cancellationToken)
    {
        var current = await LoadStateAsync(cancellationToken);
        var validationError = ValidateUpdate(request, current);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        await UpsertSettingAsync(EnabledKey, "Включен", request.Enabled?.ToString().ToLowerInvariant(), "checkbox", cancellationToken);
        await UpsertSettingAsync(ModeKey, "Режим", NormalizeMode(request.Mode), "select", cancellationToken);
        await UpsertSettingAsync(PublicBotUsernameKey, "Public bot username", NormalizeUsername(request.PublicBotUsername), "text", cancellationToken);
        await UpsertSettingAsync(WebhookUrlKey, "Webhook URL", NormalizeOptionalUrl(request.WebhookUrl), "url", cancellationToken);
        await UpsertSettingAsync(AdminChatIdKey, "Admin chat id", request.AdminChatId?.Trim(), "text", cancellationToken);
        await UpsertSettingAsync(WebAppUrlKey, "WebApp URL", NormalizeOptionalUrl(request.WebAppUrl), "url", cancellationToken);
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
        AdminAuditLogWriter.Add(
            _db,
            this,
            "telegram_bot.settings.update",
            "TelegramBotSettings",
            Guid.Empty,
            ToAuditSnapshot(current),
            ToAuditSnapshot(request));
        AddSecretRotationAudit(request);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetSettings(cancellationToken);
    }

    private async Task<TelegramBotSettingsState> LoadStateAsync(CancellationToken cancellationToken)
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

        return new TelegramBotSettingsState(
            ReadBoolSetting(settings, EnabledKey, _configuration.GetValue<bool>("TelegramBot:Enabled")),
            NormalizeMode(ReadSetting(settings, ModeKey, _configuration["TelegramBot:Mode"] ?? "LongPolling")) ?? "LongPolling",
            NormalizeUsername(ReadSetting(settings, PublicBotUsernameKey, _configuration["TelegramBot:PublicBotUsername"] ?? string.Empty)) ?? string.Empty,
            token,
            ReadSetting(settings, WebhookUrlKey, _configuration["TelegramBot:WebhookUrl"] ?? string.Empty),
            secretToken,
            ReadSetting(settings, AdminChatIdKey, _configuration["TelegramBot:AdminChatId"] ?? string.Empty),
            ReadSetting(settings, WebAppUrlKey, _configuration["TelegramBot:WebAppUrl"] ?? string.Empty),
            templates);
    }

    private static AdminTelegramBotSettingsDto ToDto(TelegramBotSettingsState state)
        => new(
            state.Enabled,
            state.Mode,
            state.PublicBotUsername,
            !string.IsNullOrWhiteSpace(state.BotToken),
            MaskToken(state.BotToken),
            state.WebhookUrl,
            !string.IsNullOrWhiteSpace(state.SecretToken),
            state.AdminChatId,
            state.WebAppUrl,
            FindTemplate(state.Templates, WelcomeKey, "Добро пожаловать! Выберите действие в меню."),
            FindTemplate(state.Templates, InstructionKey, "Инструкция появится после выдачи VPN-доступа."),
            FindTemplate(state.Templates, SupportKey, "Опишите проблему одним сообщением, оператор ответит в Telegram."),
            FindTemplate(state.Templates, AfterPaymentKey, "Оплата получена. Ваш VPN-доступ готов."),
            FindTemplate(state.Templates, RenewalKey, "Продление оформлено. После оплаты подписка будет продлена автоматически."),
            FindTemplate(state.Templates, PaymentFailedKey, "Оплата не прошла. Проверьте способ оплаты или попробуйте другой вариант."),
            FindTemplate(state.Templates, SubscriptionExpiredKey, "Срок подписки истек. Продлите тариф, чтобы восстановить VPN-доступ."),
            DateTimeOffset.UtcNow);

    private static string? ValidateUpdate(UpdateTelegramBotSettingsCommand request, TelegramBotSettingsState current)
    {
        var enabled = request.Enabled ?? current.Enabled;
        var mode = NormalizeMode(request.Mode) ?? current.Mode;
        var username = NormalizeUsername(request.PublicBotUsername) ?? current.PublicBotUsername;
        var token = string.IsNullOrWhiteSpace(request.BotToken) ? current.BotToken : request.BotToken.Trim();
        var webhookUrl = NormalizeOptionalUrl(request.WebhookUrl) ?? current.WebhookUrl;
        var webAppUrl = NormalizeOptionalUrl(request.WebAppUrl) ?? current.WebAppUrl;

        if (!string.IsNullOrWhiteSpace(username) && !IsValidTelegramUsername(username))
        {
            return "Telegram username должен содержать 5-32 символа: латиница, цифры и подчёркивание.";
        }

        if (enabled && string.IsNullOrWhiteSpace(username))
        {
            return "Для включения Telegram-бота укажите public username.";
        }

        if (enabled && string.IsNullOrWhiteSpace(token))
        {
            return "Для включения Telegram-бота укажите Bot token.";
        }

        if (string.Equals(mode, "Webhook", StringComparison.Ordinal) && enabled && string.IsNullOrWhiteSpace(webhookUrl))
        {
            return "Для режима Webhook укажите Webhook URL.";
        }

        if (!string.IsNullOrWhiteSpace(webhookUrl) && !IsHttpUrl(webhookUrl))
        {
            return "Webhook URL должен быть абсолютным http/https адресом.";
        }

        if (!string.IsNullOrWhiteSpace(webAppUrl) && !IsHttpUrl(webAppUrl))
        {
            return "WebApp URL должен быть абсолютным http/https адресом.";
        }

        return null;
    }

    private static AdminTelegramBotConnectionCheckDto BuildConnectionCheck(TelegramBotSettingsState state)
    {
        var requiredActions = new List<string>();
        var warnings = new List<string>();

        if (!state.Enabled)
        {
            requiredActions.Add("Включите Telegram-бота в настройках.");
        }

        if (string.IsNullOrWhiteSpace(state.PublicBotUsername))
        {
            requiredActions.Add("Укажите public username бота без @.");
        }
        else if (!IsValidTelegramUsername(state.PublicBotUsername))
        {
            requiredActions.Add("Исправьте public username: допустимы 5-32 символа, латиница, цифры и подчёркивание.");
        }

        if (string.IsNullOrWhiteSpace(state.BotToken))
        {
            requiredActions.Add("Укажите Bot token из BotFather.");
        }

        if (string.Equals(state.Mode, "Webhook", StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(state.WebhookUrl))
            {
                requiredActions.Add("Укажите Webhook URL для режима Webhook.");
            }
            else if (!IsHttpUrl(state.WebhookUrl))
            {
                requiredActions.Add("Исправьте Webhook URL: нужен абсолютный http/https адрес.");
            }

            if (string.IsNullOrWhiteSpace(state.SecretToken))
            {
                warnings.Add("Для Webhook рекомендуется задать secret token, чтобы отсеивать чужие запросы.");
            }
        }

        if (!string.IsNullOrWhiteSpace(state.WebAppUrl) && !IsHttpUrl(state.WebAppUrl))
        {
            requiredActions.Add("Исправьте WebApp URL: нужен абсолютный http/https адрес.");
        }

        if (string.IsNullOrWhiteSpace(state.AdminChatId))
        {
            warnings.Add("Admin chat id не задан: технические уведомления не будут уходить в админский чат.");
        }

        var isReady = requiredActions.Count == 0;
        return new AdminTelegramBotConnectionCheckDto(
            isReady,
            isReady ? "ready" : "needs_configuration",
            requiredActions,
            warnings,
            DateTimeOffset.UtcNow);
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

    private void AddSecretRotationAudit(UpdateTelegramBotSettingsCommand request)
    {
        var rotatedBotToken = !string.IsNullOrWhiteSpace(request.BotToken);
        var rotatedSecretToken = !string.IsNullOrWhiteSpace(request.SecretToken);
        if (!rotatedBotToken && !rotatedSecretToken)
        {
            return;
        }

        var afterJson = JsonSerializer.Serialize(new
        {
            rotatedBotToken,
            rotatedSecretToken,
            mode = NormalizeMode(request.Mode),
            publicBotUsername = NormalizeUsername(request.PublicBotUsername),
            webhookConfigured = !string.IsNullOrWhiteSpace(request.WebhookUrl),
            webAppConfigured = !string.IsNullOrWhiteSpace(request.WebAppUrl)
        });

        _db.AuditLogs.Add(new AuditLog
        {
            ActorType = "admin",
            ActorId = ResolveUserId()?.ToString() ?? "unknown",
            Action = "telegram_bot.secret.rotate",
            EntityType = "TelegramBotSettings",
            EntityId = SettingsGroup,
            BeforeJson = "{}",
            AfterJson = SensitiveDataRedactor.Redact(afterJson),
            Ip = HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            UserAgent = HttpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty
        });
    }

    private static object ToAuditSnapshot(TelegramBotSettingsState state)
        => new
        {
            state.Enabled,
            state.Mode,
            state.PublicBotUsername,
            botTokenConfigured = !string.IsNullOrWhiteSpace(state.BotToken),
            webhookConfigured = !string.IsNullOrWhiteSpace(state.WebhookUrl),
            secretTokenConfigured = !string.IsNullOrWhiteSpace(state.SecretToken),
            adminChatConfigured = !string.IsNullOrWhiteSpace(state.AdminChatId),
            webAppConfigured = !string.IsNullOrWhiteSpace(state.WebAppUrl),
            activeTemplateCount = state.Templates.Count(x => x.IsActive)
        };

    private static object ToAuditSnapshot(UpdateTelegramBotSettingsCommand request)
        => new
        {
            request.Enabled,
            mode = NormalizeMode(request.Mode),
            publicBotUsername = NormalizeUsername(request.PublicBotUsername),
            botTokenRotated = !string.IsNullOrWhiteSpace(request.BotToken),
            webhookConfigured = !string.IsNullOrWhiteSpace(request.WebhookUrl),
            secretTokenRotated = !string.IsNullOrWhiteSpace(request.SecretToken),
            adminChatConfigured = !string.IsNullOrWhiteSpace(request.AdminChatId),
            webAppConfigured = !string.IsNullOrWhiteSpace(request.WebAppUrl),
            templatesSubmitted = new[]
            {
                request.WelcomeText,
                request.InstructionText,
                request.SupportText,
                request.AfterPaymentTextTemplate,
                request.RenewalTextTemplate,
                request.PaymentFailedTextTemplate,
                request.SubscriptionExpiredTextTemplate
            }.Count(x => x is not null)
        };

    private Guid? ResolveUserId()
    {
        var principal = HttpContext?.User;
        if (principal is null)
        {
            return null;
        }

        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        return Guid.TryParse(raw, out var userId) ? userId : null;
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

    private static string? NormalizeOptionalUrl(string? url)
        => url is null ? null : url.Trim();

    private static bool IsHttpUrl(string url)
        => Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static bool IsValidTelegramUsername(string username)
    {
        var normalized = username.Trim().TrimStart('@');
        if (normalized.Length is < 5 or > 32)
        {
            return false;
        }

        return normalized.All(ch => char.IsAsciiLetterOrDigit(ch) || ch == '_');
    }

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

    private sealed record TelegramBotSettingsState(
        bool Enabled,
        string Mode,
        string PublicBotUsername,
        string BotToken,
        string WebhookUrl,
        string SecretToken,
        string AdminChatId,
        string WebAppUrl,
        IReadOnlyCollection<NotificationTemplate> Templates);
}
