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

        if (await _db.WorkScenarios.AnyAsync(x => x.Key == scenario.Key, cancellationToken))
        {
            return BadRequest(new { error = "Scenario key already exists." });
        }

        _db.WorkScenarios.Add(scenario);
        AdminAuditLogWriter.Add(_db, this, "work_scenario.create", "WorkScenario", scenario.Id, null, Map(scenario));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(Map(scenario));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AdminPolicies.AdminWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] WorkScenarioUpsertRequest request, CancellationToken cancellationToken)
    {
        var scenario = await _db.WorkScenarios.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (scenario is null) return NotFound();

        var candidate = new WorkScenario();
        var error = Apply(candidate, request);
        if (error is not null) return BadRequest(new { error });

        if (await _db.WorkScenarios.AnyAsync(x => x.Id != id && x.Key == candidate.Key, cancellationToken))
        {
            return BadRequest(new { error = "Scenario key already exists." });
        }

        if (!string.Equals(scenario.Key, candidate.Key, StringComparison.Ordinal)
            && await _db.Tariffs.AnyAsync(x => x.ProvisioningScenario == scenario.Key, cancellationToken))
        {
            return BadRequest(new { error = "Scenario key cannot be changed while the scenario is selected in tariffs." });
        }

        var before = Map(scenario);
        Copy(candidate, scenario);
        scenario.UpdatedAt = DateTimeOffset.UtcNow;
        AdminAuditLogWriter.Add(_db, this, "work_scenario.update", "WorkScenario", scenario.Id, before, Map(scenario));
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

        var before = Map(scenario);
        _db.WorkScenarios.Remove(scenario);
        AdminAuditLogWriter.Add(_db, this, "work_scenario.delete", "WorkScenario", scenario.Id, before, null);
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
        var allowedTariffIds = NormalizeGuidArrayJson(request.AllowedTariffIdsJson);
        if (allowedTariffIds.Error is not null) return allowedTariffIds.Error;

        scenario.AllowedTariffIdsJson = allowedTariffIds.Json;
        scenario.VpnProtocol = string.IsNullOrWhiteSpace(request.VpnProtocol) ? "vless" : request.VpnProtocol.Trim();
        scenario.ServerSelectionRule = string.IsNullOrWhiteSpace(request.ServerSelectionRule) ? "least-loaded" : request.ServerSelectionRule.Trim();
        scenario.InboundSelectionRule = string.IsNullOrWhiteSpace(request.InboundSelectionRule) ? "default" : request.InboundSelectionRule.Trim();
        scenario.ProvisioningMode = NormalizeScenarioToken(request.ProvisioningMode, "auto");
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

    private static void Copy(WorkScenario source, WorkScenario target)
    {
        target.Name = source.Name;
        target.Key = source.Key;
        target.IsActive = source.IsActive;
        target.AllowedTariffIdsJson = source.AllowedTariffIdsJson;
        target.VpnProtocol = source.VpnProtocol;
        target.ServerSelectionRule = source.ServerSelectionRule;
        target.InboundSelectionRule = source.InboundSelectionRule;
        target.ProvisioningMode = source.ProvisioningMode;
        target.OnPaymentSucceeded = source.OnPaymentSucceeded;
        target.OnPaymentFailed = source.OnPaymentFailed;
        target.OnRefund = source.OnRefund;
        target.OnSubscriptionExpired = source.OnSubscriptionExpired;
        target.OnRenewal = source.OnRenewal;
        target.CabinetText = source.CabinetText;
        target.TelegramText = source.TelegramText;
        target.GenerateQrCode = source.GenerateQrCode;
        target.MaxDevices = source.MaxDevices;
        target.TrafficLimit = source.TrafficLimit;
        target.SortOrder = source.SortOrder;
    }

    private static string NormalizeScenarioToken(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();

    private static string NormalizeKey(string value)
    {
        var key = Regex.Replace(value.Trim().ToLowerInvariant(), @"\s+", "-");
        key = Regex.Replace(key, @"[^a-z0-9\-_]+", "-");
        key = Regex.Replace(key, @"-+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(key) ? "scenario" : key;
    }

    private static (string Json, string? Error) NormalizeGuidArrayJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return ("[]", null);

        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return ("[]", "Allowed tariff ids must be a JSON array.");
            }

            var ids = new List<Guid>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String || !Guid.TryParse(item.GetString(), out var id))
                {
                    return ("[]", "Allowed tariff ids must contain only tariff GUID strings.");
                }

                if (!ids.Contains(id))
                {
                    ids.Add(id);
                }
            }

            return (JsonSerializer.Serialize(ids), null);
        }
        catch (JsonException)
        {
            return ("[]", "Allowed tariff ids must be valid JSON.");
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
