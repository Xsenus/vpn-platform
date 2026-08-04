using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;

namespace VpnPlatform.Application.Services;

public sealed class VpnNodeCapacityService
{
    private readonly IApplicationDbContext _db;

    public VpnNodeCapacityService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> TryReserveAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        if (IsInMemoryProvider())
        {
            var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == nodeId, cancellationToken);
            if (node is null || node.UsedCapacity >= node.Capacity)
            {
                return false;
            }

            node.UsedCapacity += 1;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        var affected = await _db.VpnNodes
            .Where(x => x.Id == nodeId && x.UsedCapacity < x.Capacity)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.UsedCapacity, x => x.UsedCapacity + 1),
                cancellationToken);
        return affected == 1;
    }

    public async Task<bool> ReleaseAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        if (IsInMemoryProvider())
        {
            var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == nodeId, cancellationToken);
            if (node is null || node.UsedCapacity <= 0)
            {
                return false;
            }

            node.UsedCapacity -= 1;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        var affected = await _db.VpnNodes
            .Where(x => x.Id == nodeId && x.UsedCapacity > 0)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.UsedCapacity, x => x.UsedCapacity - 1),
                cancellationToken);
        return affected == 1;
    }

    private bool IsInMemoryProvider()
        => _db is DbContext dbContext
            && string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal);
}
