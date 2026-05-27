using System.Text.Json;
using System.Text.RegularExpressions;
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
[Route("api/admin/work-scenarios")]
public class AdminWorkScenariosController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public AdminWorkScenariosController(IApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
    {
        var scenarios = await _db.WorkScenarios
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return Ok(scenarios.Select(Map).ToList());
    }

    [HttpPost]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> Create([FromBody] WorkScenarioUpsertRequest request, CancellationToken cancellationToken)
    {
        var scenario = new WorkScenario();
        var error = Apply(scenario, request);
        if (error is not null) return BadRequest(new { error });

        _db.WorkScenarios.Add(scenario);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(Map(scenario));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] WorkScenarioUpsertRequest request, CancellationToken cancellationToken)
    {
        var scenario = await _db.WorkScenarios.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (scenario is null) return NotFound();

        var error = Apply(scenario, request);
        if (error is not null) return BadRequest(new { error });

        scenario.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(Map(scenario));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var scenario = await _db.WorkScenarios.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (scenario is null) return NotFound();

        var linkedTariffs = await _db.Tariffs.AnyAsync(x => x.ProvisioningScenario == scenario.Key, cancellationToken);
        if (linkedTariffs)
        {
            return BadRequest(new { error = "Нельзя удалить сценарий, который выбран в тарифах. Сначала смените сценарий у связанных тарифов." });
        }

        _db.WorkScenarios.Remove(scenario);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { id, deleted = true });
    }

    private static string? Apply(WorkScenario scenario, WorkScenarioUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return "Scenario name is required.";
        if (string.IsNullOrWhiteSpace(request.Key)) return "Scenario key is required.";
        if (request.MaxDevices <= 0) return "Scenario maxDevices must be positive.";

        scenario.Name = request.Name.Trim();
        scenario.Key = NormalizeKey(request.Key);
        scenario.IsActive = request.IsActive;
        scenario.AllowedTariffIdsJson = NormalizeJsonArray(request.AllowedTariffIdsJson);
        scenario.VpnProtocol = string.IsNullOrWhiteSpace(request.VpnProtocol) ? "vless" : request.VpnProtocol.Trim();
        scenario.ServerSelectionRule = string.IsNullOrWhiteSpace(request.ServerSelectionRule) ? "least-loaded" : request.ServerSelectionRule.Trim();
        scenario.InboundSelectionRule = string.IsNullOrWhiteSpace(request.InboundSelectionRule) ? "default" : request.InboundSelectionRule.Trim();
        scenario.ProvisioningMode = string.IsNullOrWhiteSpace(request.ProvisioningMode) ? "auto" : request.ProvisioningMode.Trim();
        scenario.OnPaymentSucceeded = NormalizeText(request.OnPaymentSucceeded, "create_subscription_and_access");
        scenario.OnPaymentFailed = NormalizeText(request.OnPaymentFailed, "keep_order_pending");
        scenario.OnRefund = NormalizeText(request.OnRefund, "disable_access");
        scenario.OnSubscriptionExpired = NormalizeText(request.OnSubscriptionExpired, "disable_access_after_grace");
        scenario.OnRenewal = NormalizeText(request.OnRenewal, "extend_subscription");
        scenario.CabinetText = request.CabinetText?.Trim() ?? string.Empty;
        scenario.TelegramText = request.TelegramText?.Trim() ?? string.Empty;
        scenario.GenerateQrCode = request.GenerateQrCode;
        scenario.MaxDevices = request.MaxDevices;
        scenario.TrafficLimit = request.TrafficLimit;
        scenario.SortOrder = request.SortOrder;
        return null;
    }

    private static string NormalizeText(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string NormalizeKey(string value)
    {
        var key = Regex.Replace(value.Trim().ToLowerInvariant(), @"\s+", "-");
        key = Regex.Replace(key, @"[^a-z0-9\-_]+", "-");
        key = Regex.Replace(key, @"-+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(key) ? "scenario" : key;
    }

    private static string NormalizeJsonArray(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "[]";

        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind == JsonValueKind.Array ? value : "[]";
        }
        catch (JsonException)
        {
            return "[]";
        }
    }

    private static WorkScenarioDto Map(WorkScenario scenario)
        => new(
            scenario.Id,
            scenario.Name,
            scenario.Key,
            scenario.IsActive,
            scenario.AllowedTariffIdsJson,
            scenario.VpnProtocol,
            scenario.ServerSelectionRule,
            scenario.InboundSelectionRule,
            scenario.ProvisioningMode,
            scenario.OnPaymentSucceeded,
            scenario.OnPaymentFailed,
            scenario.OnRefund,
            scenario.OnSubscriptionExpired,
            scenario.OnRenewal,
            scenario.CabinetText,
            scenario.TelegramText,
            scenario.GenerateQrCode,
            scenario.MaxDevices,
            scenario.TrafficLimit,
            scenario.SortOrder,
            scenario.CreatedAt,
            scenario.UpdatedAt);
}
