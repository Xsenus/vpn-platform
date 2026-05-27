using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;

namespace VpnPlatform.Application.Services;

public class CatalogService
{
    private readonly IApplicationDbContext _db;

    public CatalogService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<TariffDto>> GetPublicTariffsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var tariffs = await _db.Tariffs
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return tariffs
            .Where(x => x.IsActive && (x.VisibleFrom == null || x.VisibleFrom <= now) && (x.VisibleTo == null || x.VisibleTo >= now))
            .OrderBy(x => x.SortOrder)
            .Select(MapTariff)
            .ToList();
    }

    private static TariffDto MapTariff(Tariff tariff)
        => new(
            tariff.Id,
            tariff.Name,
            tariff.Slug,
            tariff.Description,
            tariff.FullDescription,
            ParseFeatures(tariff.FeaturesJson),
            tariff.FeaturesJson,
            tariff.Badge,
            tariff.DurationDays,
            tariff.Price,
            tariff.Currency,
            tariff.MaxDevices,
            tariff.TrafficLimit,
            tariff.IsTrial,
            tariff.IsActive,
            tariff.SortOrder,
            tariff.VisibleFrom,
            tariff.VisibleTo,
            tariff.TariffType.ToString(),
            tariff.Category,
            tariff.AllowedRegionsCsv,
            tariff.AllowedNodeGroupsCsv,
            tariff.IsReferralEligible,
            tariff.ProvisioningScenario,
            tariff.AfterPaymentText,
            tariff.CreatedAt,
            tariff.UpdatedAt);

    private static IReadOnlyList<string> ParseFeatures(string? featuresJson)
    {
        if (string.IsNullOrWhiteSpace(featuresJson)) return [];

        try
        {
            var items = JsonSerializer.Deserialize<List<string>>(featuresJson);
            return items?
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
