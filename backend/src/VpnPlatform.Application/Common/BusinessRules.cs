using VpnPlatform.Domain.Enums;

namespace VpnPlatform.Application.Common;

public static class BusinessRules
{
    public static bool IsOrderExpired(DateTimeOffset now, DateTimeOffset expiresAt)
        => expiresAt <= now;

    public static DateTimeOffset GetRenewalBaseDate(DateTimeOffset now, DateTimeOffset currentEndAt)
        => currentEndAt > now ? currentEndAt : now;

    public static DateTimeOffset GetGracePeriodEnd(DateTimeOffset endAt, int graceDays = 3)
        => endAt.AddDays(graceDays);

    public static DateTimeOffset GetSubscriptionAccessEnd(DateTimeOffset endAt, DateTimeOffset? gracePeriodEndAt)
        => gracePeriodEndAt ?? endAt;

    public static bool IsSubscriptionAccessAvailable(
        SubscriptionStatus status,
        DateTimeOffset endAt,
        DateTimeOffset? gracePeriodEndAt,
        DateTimeOffset now)
        => status is SubscriptionStatus.Active or SubscriptionStatus.GracePeriod
            && GetSubscriptionAccessEnd(endAt, gracePeriodEndAt) > now;
}
