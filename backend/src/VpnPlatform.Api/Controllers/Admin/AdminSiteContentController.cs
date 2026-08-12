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
    private const int ListLimit = 200;
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
    private static readonly string[] RequiredHomeKeys = RequiredHomeDefaults.Select(item => item.Key).ToArray();
    private static readonly string[] NormalizedRequiredHomeKeys = RequiredHomeKeys.Select(key => key.ToLowerInvariant()).ToArray();

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
            .Take(ListLimit)
            .ToListAsync(cancellationToken);

        return Ok(blocks.Select(Map).ToList());
    }

    [HttpGet("home-readiness")]
    public async Task<IActionResult> GetHomeReadiness(CancellationToken cancellationToken = default)
    {
        return Ok(await BuildHomeReadiness(cancellationToken));
    }

    [HttpPost("home-defaults")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> RestoreHomeDefaults(CancellationToken cancellationToken)
    {
        var created = 0;
        var restored = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var item in RequiredHomeDefaults)
        {
            var normalizedKey = item.Key.ToLowerInvariant();
            var block = await _db.SiteContentBlocks
                .Where(candidate => candidate.Key.ToLower() == normalizedKey)
                .OrderBy(candidate => candidate.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (block is null)
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
                block.Revision++;
                restored++;
            }
        }

        AdminAuditLogWriter.Add(
            _db,
            this,
            "site_content.home_defaults.restore",
            "SiteContentDefaults",
            Guid.Empty,
            null,
            new { created, restored });
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Site content changed. Reload it and retry." });
        }

        return Ok(new { created, restored, readiness = await BuildHomeReadiness(cancellationToken) });
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
        AdminAuditLogWriter.Add(_db, this, "site_content.create", "SiteContentBlock", block.Id, null, Map(block));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(Map(block));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SiteContentBlockUpsertRequest request, CancellationToken cancellationToken)
    {
        var block = await _db.SiteContentBlocks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (block is null) return NotFound();
        if (!request.Revision.HasValue || request.Revision.Value < 0)
        {
            return BadRequest(new { error = "Site content revision is required and must be a non-negative integer." });
        }
        if (request.Revision.Value != block.Revision)
        {
            return Conflict(new { error = "Site content changed. Reload it and retry.", revision = block.Revision });
        }

        var candidate = new SiteContentBlock();
        var error = Apply(candidate, request);
        if (error is not null) return BadRequest(new { error });
        if (await _db.SiteContentBlocks.AsNoTracking().AnyAsync(x => x.Id != id && x.Key == candidate.Key, cancellationToken))
        {
            return BadRequest(new { error = "Content key already exists." });
        }

        var before = Map(block);
        Copy(candidate, block);
        block.Revision++;
        block.UpdatedAt = DateTimeOffset.UtcNow;
        AdminAuditLogWriter.Add(_db, this, "site_content.update", "SiteContentBlock", block.Id, before, Map(block));
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Site content changed. Reload it and retry." });
        }
        return Ok(Map(block));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] int? revision, CancellationToken cancellationToken)
    {
        var block = await _db.SiteContentBlocks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (block is null) return NotFound();
        if (!revision.HasValue || revision.Value < 0)
        {
            return BadRequest(new { error = "Site content revision is required and must be a non-negative integer." });
        }
        if (revision.Value != block.Revision)
        {
            return Conflict(new { error = "Site content changed. Reload it and retry.", revision = block.Revision });
        }

        var before = Map(block);
        _db.SiteContentBlocks.Remove(block);
        AdminAuditLogWriter.Add(_db, this, "site_content.delete", "SiteContentBlock", block.Id, before, null);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Site content changed. Reload it and retry." });
        }
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

    private static void Copy(SiteContentBlock source, SiteContentBlock target)
    {
        target.Key = source.Key;
        target.Value = source.Value;
        target.Group = source.Group;
        target.Label = source.Label;
        target.Description = source.Description;
        target.InputType = source.InputType;
        target.IsActive = source.IsActive;
        target.SortOrder = source.SortOrder;
    }

    private static SiteContentBlockDto Map(SiteContentBlock block)
        => new(block.Id, block.Revision, block.Key, block.Value, block.Group, block.Label, block.Description, block.InputType, block.IsActive, block.SortOrder, block.CreatedAt, block.UpdatedAt);

    private async Task<object> BuildHomeReadiness(CancellationToken cancellationToken)
    {
        var requiredStatuses = await _db.SiteContentBlocks.AsNoTracking()
            .Where(x => NormalizedRequiredHomeKeys.Contains(x.Key.ToLower()))
            .GroupBy(x => x.Key.ToLower())
            .Select(group => new
            {
                Key = group.Key,
                Count = group.Count(),
                HasInactive = group.Any(block => !block.IsActive),
                HasEmpty = group.Any(block => block.Value.Trim() == ""),
                HasActiveValue = group.Any(block => block.IsActive && block.Value.Trim() != "")
            })
            .ToListAsync(cancellationToken);
        var statusByKey = requiredStatuses.ToDictionary(status => status.Key, StringComparer.OrdinalIgnoreCase);
        var duplicate = await _db.SiteContentBlocks.AsNoTracking()
            .Where(x => x.Group == "home" || x.Key.StartsWith("home."))
            .GroupBy(x => x.Key.ToLower())
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key)
            .Select(group => group.Key)
            .Take(ListLimit)
            .ToArrayAsync(cancellationToken);
        var publicBlocksCount = await _db.SiteContentBlocks.AsNoTracking()
            .CountAsync(x => x.Group == "home" && x.IsActive, cancellationToken);
        var missing = RequiredHomeKeys.Where(key => !statusByKey.ContainsKey(key)).ToArray();
        var inactive = RequiredHomeKeys.Where(key => statusByKey.TryGetValue(key, out var status) && status.HasInactive).ToArray();
        var empty = RequiredHomeKeys.Where(key => statusByKey.TryGetValue(key, out var status) && status.HasEmpty).ToArray();
        var required = RequiredHomeDefaults.Count;
        var ready = missing.Length == 0 && inactive.Length == 0 && empty.Length == 0 && duplicate.Length == 0;

        return new
        {
            IsReady = ready,
            RequiredCount = required,
            PresentCount = requiredStatuses.Count,
            ActiveRequiredCount = requiredStatuses.Count(status => status.HasActiveValue),
            MissingKeys = missing,
            InactiveKeys = inactive,
            EmptyKeys = empty,
            DuplicateKeys = duplicate,
            PublicBlocksCount = publicBlocksCount,
            RequiredKeys = RequiredHomeKeys
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
