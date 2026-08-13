using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Domain.Entities;

namespace VpnPlatform.Application.Services;

public class CatalogService
{
    private const int PublicTariffLimit = 200;
    private readonly IApplicationDbContext _db;

    public CatalogService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<PublicTariffDto>> GetPublicTariffsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        IQueryable<Tariff> query;

        if (_db is DbContext dbContext
            && string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
        {
            query = _db.Tariffs.FromSqlInterpolated($"""
                SELECT t.*
                FROM "Tariffs" AS t
                WHERE t."IsActive" = 1
                  AND (t."VisibleFrom" IS NULL OR julianday(t."VisibleFrom") <= julianday({now}))
                  AND (t."VisibleTo" IS NULL OR julianday(t."VisibleTo") >= julianday({now}))
                """);
        }
        else
        {
            query = _db.Tariffs.Where(x => x.IsActive
                && (x.VisibleFrom == null || x.VisibleFrom <= now)
                && (x.VisibleTo == null || x.VisibleTo >= now));
        }

        var tariffs = await query
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Take(PublicTariffLimit)
            .ToListAsync(cancellationToken);

        return tariffs
            .Select(MapTariff)
            .ToList();
    }

    private static PublicTariffDto MapTariff(Tariff tariff)
        => new(
            tariff.Id,
            tariff.Name,
            tariff.Slug,
            tariff.Description,
            tariff.FullDescription,
            ParseFeatures(tariff.FeaturesJson),
            tariff.Badge,
            tariff.DurationDays,
            tariff.Price,
            tariff.Currency,
            tariff.MaxDevices,
            tariff.TrafficLimit,
            tariff.Category,
            tariff.AfterPaymentText);

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
