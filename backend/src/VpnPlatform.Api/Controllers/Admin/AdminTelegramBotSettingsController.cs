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

    private readonly IApplicationDbContext _db;
    private readonly IConfiguration _configuration;

    public AdminTelegramBotSettingsController(IApplicationDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var templates = await _db.NotificationTemplates
            .AsNoTracking()
            .Where(x => x.Channel == NotificationChannelType.Telegram && x.Language == "ru")
            .ToListAsync(cancellationToken);

        var token = _configuration["TelegramBot:BotToken"] ?? string.Empty;
        var secretToken = _configuration["TelegramBot:SecretToken"] ?? string.Empty;

        return Ok(new AdminTelegramBotSettingsDto(
            _configuration.GetValue<bool>("TelegramBot:Enabled"),
            _configuration["TelegramBot:Mode"] ?? "Polling",
            _configuration["TelegramBot:PublicBotUsername"] ?? string.Empty,
            !string.IsNullOrWhiteSpace(token),
            MaskToken(token),
            _configuration["TelegramBot:WebhookUrl"] ?? string.Empty,
            !string.IsNullOrWhiteSpace(secretToken),
            FindTemplate(templates, WelcomeKey, "Добро пожаловать! Выберите действие в меню."),
            FindTemplate(templates, InstructionKey, "Инструкция появится после выдачи VPN-доступа."),
            FindTemplate(templates, SupportKey, "Опишите проблему одним сообщением, оператор ответит в Telegram."),
            FindTemplate(templates, AfterPaymentKey, "Оплата получена. Ваш VPN-доступ готов."),
            DateTimeOffset.UtcNow));
    }

    [HttpPatch("settings")]
    [Authorize(Policy = AdminPolicies.BotManage)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateTelegramBotSettingsCommand request, CancellationToken cancellationToken)
    {
        await UpsertTemplateAsync(WelcomeKey, "Welcome text", request.WelcomeText, cancellationToken);
        await UpsertTemplateAsync(InstructionKey, "Instruction text", request.InstructionText, cancellationToken);
        await UpsertTemplateAsync(SupportKey, "Support text", request.SupportText, cancellationToken);
        await UpsertTemplateAsync(AfterPaymentKey, "After payment text", request.AfterPaymentTextTemplate, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetSettings(cancellationToken);
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
