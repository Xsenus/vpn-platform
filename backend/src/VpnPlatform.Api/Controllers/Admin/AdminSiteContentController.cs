using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;

namespace VpnPlatform.Api.Controllers.Admin;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminRead)]
[Route("api/admin/site-content")]
public class AdminSiteContentController : ControllerBase
{
    private static readonly IReadOnlyList<SiteContentDefault> RequiredHomeDefaults =
    [
        new("home.hero.eyebrow", "VPN Platform", "Hero eyebrow", "Надзаголовок первого экрана", 10),
        new("home.hero.title", "Быстрый VPN-доступ с оплатой и автоматической выдачей", "Hero title", "Главный заголовок лендинга", 20),
        new("home.hero.subtitle", "Выберите тариф, оплатите удобным способом и получите готовую ссылку подключения. Платформа объединяет витрину, личный кабинет, Telegram-бота, платежи, тарифы и управление серверами.", "Hero subtitle", "Текст под главным заголовком", 30, "textarea"),
        new("home.hero.primaryCta", "Выбрать тариф", "Основная CTA", "Текст кнопки перехода к тарифам", 40),
        new("home.hero.secondaryCta", "Войти или зарегистрироваться", "Вторичная CTA", "Текст кнопки перехода в аккаунт", 50),
        new("home.seo.title", "VPN Platform — быстрый VPN-доступ с автоматической выдачей", "SEO title", "Заголовок страницы для браузера и поисковиков", 60),
        new("home.seo.description", "Купите VPN-доступ онлайн: тарифы, оплата, личный кабинет, Telegram-бот и автоматическая выдача подключения.", "SEO description", "Описание главной страницы для поисковиков", 70, "textarea"),
        new("home.features.title", "Все ключевые сценарии продажи VPN в одной системе", "Заголовок возможностей", "Заголовок блока возможностей", 110),
        new("home.features.subtitle", "Лендинг ведет пользователя к тарифу, кабинет помогает завершить покупку, а админка дает контроль над тарифами, провайдерами, ботами, серверами и выдачей доступа.", "Описание возможностей", "Описание блока возможностей", 120, "textarea"),
        new("home.features.item1", "Автоматическая выдача VPN-доступа после подтверждения оплаты.", "Преимущество 1", "Пункт списка преимуществ на главной", 130, "textarea"),
        new("home.features.item2", "Тарифы, платежи, Telegram-боты и серверы управляются из админки.", "Преимущество 2", "Пункт списка преимуществ на главной", 140, "textarea"),
        new("home.features.item3", "Поддержка нескольких платежных провайдеров и безопасного sandbox-режима.", "Преимущество 3", "Пункт списка преимуществ на главной", 150, "textarea"),
        new("home.features.item4", "Личный кабинет хранит заказы, ссылки подключения и статус подписки.", "Преимущество 4", "Пункт списка преимуществ на главной", 160, "textarea"),
        new("home.pricing.title", "Понятные планы для разных сценариев", "Заголовок тарифов", "Заголовок preview-блока тарифов", 210),
        new("home.finalCta.title", "Готовы проверить покупку VPN?", "Финальный CTA", "Заголовок финального призыва", 510),
        new("home.finalCta.subtitle", "Начните с тарифа или войдите в кабинет, чтобы привязать заказ и получить ссылку подключения.", "Описание финального CTA", "Текст финального призыва", 520, "textarea"),
        new("home.footer.text", "VPN Platform объединяет продажи, оплату, выдачу и поддержку VPN-доступов в одном интерфейсе.", "Footer text", "Основной текст footer главной страницы", 610, "textarea"),
        new("home.checkout.afterPaymentText", "После оплаты вернитесь в кабинет: статус заказа обновится автоматически, а VPN-доступ появится после подтверждения платежа.", "Текст после оплаты", "Инструкция в блоке созданной покупки", 830, "textarea")
    ];

    private readonly IApplicationDbContext _db;

    public AdminSiteContentController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? group = null, CancellationToken cancellationToken = default)
    {
        var query = _db.SiteContentBlocks.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(group))
        {
            var normalizedGroup = group.Trim();
            query = query.Where(x => x.Group == normalizedGroup);
        }

        var blocks = await query
            .OrderBy(x => x.Group)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Key)
            .ToListAsync(cancellationToken);

        return Ok(blocks.Select(Map).ToList());
    }

    [HttpGet("home-readiness")]
    public async Task<IActionResult> GetHomeReadiness(CancellationToken cancellationToken = default)
    {
        var blocks = await _db.SiteContentBlocks.AsNoTracking()
            .Where(x => x.Group == "home" || x.Key.StartsWith("home."))
            .ToListAsync(cancellationToken);
        return Ok(BuildHomeReadiness(blocks));
    }

    [HttpPost("home-defaults")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> RestoreHomeDefaults(CancellationToken cancellationToken)
    {
        var blocks = await _db.SiteContentBlocks
            .Where(x => x.Group == "home" || x.Key.StartsWith("home."))
            .ToListAsync(cancellationToken);
        var byKey = blocks
            .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var created = 0;
        var restored = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var item in RequiredHomeDefaults)
        {
            if (!byKey.TryGetValue(item.Key, out var block))
            {
                block = item.ToEntity();
                _db.SiteContentBlocks.Add(block);
                created++;
                continue;
            }

            var changed = false;
            if (string.IsNullOrWhiteSpace(block.Value))
            {
                block.Value = item.Value;
                changed = true;
            }

            if (!block.IsActive)
            {
                block.IsActive = true;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(block.Label))
            {
                block.Label = item.Label;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(block.Description))
            {
                block.Description = item.Description;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(block.InputType))
            {
                block.InputType = item.InputType;
                changed = true;
            }

            if (block.Group != "home")
            {
                block.Group = "home";
                changed = true;
            }

            if (block.SortOrder == 0)
            {
                block.SortOrder = item.SortOrder;
                changed = true;
            }

            if (changed)
            {
                block.UpdatedAt = now;
                restored++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        var nextBlocks = await _db.SiteContentBlocks.AsNoTracking()
            .Where(x => x.Group == "home" || x.Key.StartsWith("home."))
            .ToListAsync(cancellationToken);
        return Ok(new { created, restored, readiness = BuildHomeReadiness(nextBlocks) });
    }

    [HttpPost]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> Create([FromBody] SiteContentBlockUpsertRequest request, CancellationToken cancellationToken)
    {
        var block = new SiteContentBlock();
        var error = Apply(block, request);
        if (error is not null) return BadRequest(new { error });
        if (await _db.SiteContentBlocks.AsNoTracking().AnyAsync(x => x.Key == block.Key, cancellationToken))
        {
            return BadRequest(new { error = "Content key already exists." });
        }

        _db.SiteContentBlocks.Add(block);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(Map(block));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SiteContentBlockUpsertRequest request, CancellationToken cancellationToken)
    {
        var block = await _db.SiteContentBlocks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (block is null) return NotFound();

        var error = Apply(block, request);
        if (error is not null) return BadRequest(new { error });
        if (await _db.SiteContentBlocks.AsNoTracking().AnyAsync(x => x.Id != id && x.Key == block.Key, cancellationToken))
        {
            return BadRequest(new { error = "Content key already exists." });
        }

        block.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(Map(block));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var block = await _db.SiteContentBlocks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (block is null) return NotFound();

        _db.SiteContentBlocks.Remove(block);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { id, deleted = true });
    }

    private static string? Apply(SiteContentBlock block, SiteContentBlockUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Key)) return "Content key is required.";
        if (string.IsNullOrWhiteSpace(request.Label)) return "Content label is required.";

        block.Key = request.Key.Trim();
        block.Value = request.Value ?? string.Empty;
        block.Group = string.IsNullOrWhiteSpace(request.Group) ? "home" : request.Group.Trim();
        block.Label = request.Label.Trim();
        block.Description = request.Description?.Trim() ?? string.Empty;
        block.InputType = string.IsNullOrWhiteSpace(request.InputType) ? "text" : request.InputType.Trim();
        block.IsActive = request.IsActive;
        block.SortOrder = request.SortOrder;
        return null;
    }

    private static SiteContentBlockDto Map(SiteContentBlock block)
        => new(block.Id, block.Key, block.Value, block.Group, block.Label, block.Description, block.InputType, block.IsActive, block.SortOrder, block.CreatedAt, block.UpdatedAt);

    private static object BuildHomeReadiness(IReadOnlyCollection<SiteContentBlock> blocks)
    {
        var byKey = blocks.ToLookup(x => x.Key, StringComparer.OrdinalIgnoreCase);
        var missing = RequiredHomeDefaults.Where(x => !byKey.Contains(x.Key)).Select(x => x.Key).ToArray();
        var inactive = blocks.Where(x => RequiredHomeDefaults.Any(required => required.Key.Equals(x.Key, StringComparison.OrdinalIgnoreCase)) && !x.IsActive).Select(x => x.Key).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var empty = blocks.Where(x => RequiredHomeDefaults.Any(required => required.Key.Equals(x.Key, StringComparison.OrdinalIgnoreCase)) && string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Key).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var duplicate = byKey.Where(x => x.Count() > 1).Select(x => x.Key).ToArray();
        var required = RequiredHomeDefaults.Count;
        var ready = missing.Length == 0 && inactive.Length == 0 && empty.Length == 0 && duplicate.Length == 0;

        return new
        {
            IsReady = ready,
            RequiredCount = required,
            PresentCount = RequiredHomeDefaults.Count(x => byKey.Contains(x.Key)),
            ActiveRequiredCount = RequiredHomeDefaults.Count(x => byKey[x.Key].Any(block => block.IsActive && !string.IsNullOrWhiteSpace(block.Value))),
            MissingKeys = missing,
            InactiveKeys = inactive,
            EmptyKeys = empty,
            DuplicateKeys = duplicate,
            PublicBlocksCount = blocks.Count(x => x.Group == "home" && x.IsActive),
            RequiredKeys = RequiredHomeDefaults.Select(x => x.Key).ToArray()
        };
    }

    private sealed record SiteContentDefault(string Key, string Value, string Label, string Description, int SortOrder, string InputType = "text")
    {
        public SiteContentBlock ToEntity()
            => new()
            {
                Key = Key,
                Value = Value,
                Group = "home",
                Label = Label,
                Description = Description,
                InputType = InputType,
                IsActive = true,
                SortOrder = SortOrder
            };
    }
}
