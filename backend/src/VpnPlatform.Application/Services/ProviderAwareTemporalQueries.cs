using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Services;

internal static class ProviderAwareTemporalQueries
{
    public static async Task<AccessCredential?> GetLatestAccessCredentialAsync(
        IApplicationDbContext db,
        Guid subscriptionId,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        if (db is DbContext dbContext
            && string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
        {
            var query = activeOnly
                ? db.AccessCredentials.FromSqlInterpolated($"""
                    SELECT *
                    FROM "AccessCredentials"
                    WHERE "SubscriptionId" = {subscriptionId}
                      AND "Status" = {AccessCredentialStatus.Active}
                    ORDER BY julianday("IssuedAt") DESC, "Id" DESC
                    LIMIT 1
                    """)
                : db.AccessCredentials.FromSqlInterpolated($"""
                    SELECT *
                    FROM "AccessCredentials"
                    WHERE "SubscriptionId" = {subscriptionId}
                    ORDER BY julianday("IssuedAt") DESC, "Id" DESC
                    LIMIT 1
                    """);

            return await query.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        }

        var accessQuery = db.AccessCredentials.AsNoTracking()
            .Where(x => x.SubscriptionId == subscriptionId);
        if (activeOnly)
        {
            accessQuery = accessQuery.Where(x => x.Status == AccessCredentialStatus.Active);
        }

        return await accessQuery
            .OrderByDescending(x => x.IssuedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
