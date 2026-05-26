using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;

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
            .Select(x => new TariffDto(x.Id, x.Name, x.Slug, x.Description, x.DurationDays, x.Price, x.Currency, x.MaxDevices, x.Category))
            .ToList();
    }
}
