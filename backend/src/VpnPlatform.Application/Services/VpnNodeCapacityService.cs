using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Common;
using VpnPlatform.Domain.Enums;

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
        await using var gate = await PaymentProcessingGate.AcquireVpnNodeStateAsync(nodeId, cancellationToken);
        if (IsInMemoryProvider())
        {
            var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == nodeId, cancellationToken);
            if (node is null
                || node.Status != NodeStatus.Ready
                || !node.IsAvailableForNewUsers
                || node.HealthStatus == HealthStatus.Unhealthy
                || node.UsedCapacity >= node.Capacity)
            {
                return false;
            }

            node.UsedCapacity += 1;
            node.Revision = checked(node.Revision + 1);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        var affected = await _db.VpnNodes
            .Where(x => x.Id == nodeId
                && x.Status == NodeStatus.Ready
                && x.IsAvailableForNewUsers
                && x.HealthStatus != HealthStatus.Unhealthy
                && x.UsedCapacity < x.Capacity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.UsedCapacity, x => x.UsedCapacity + 1)
                    .SetProperty(x => x.Revision, x => x.Revision + 1),
                cancellationToken);
        return affected == 1;
    }

    public async Task<bool> ReleaseAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        await using var gate = await PaymentProcessingGate.AcquireVpnNodeStateAsync(nodeId, cancellationToken);
        if (IsInMemoryProvider())
        {
            var node = await _db.VpnNodes.FirstOrDefaultAsync(x => x.Id == nodeId, cancellationToken);
            if (node is null || node.UsedCapacity <= 0)
            {
                return false;
            }

            node.UsedCapacity -= 1;
            node.Revision = checked(node.Revision + 1);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        var affected = await _db.VpnNodes
            .Where(x => x.Id == nodeId && x.UsedCapacity > 0)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.UsedCapacity, x => x.UsedCapacity - 1)
                    .SetProperty(x => x.Revision, x => x.Revision + 1),
                cancellationToken);
        return affected == 1;
    }

    private bool IsInMemoryProvider()
        => _db is DbContext dbContext
            && string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal);
}
