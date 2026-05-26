namespace VpnPlatform.Application.Common;

public static class BusinessRules
{
    public static bool IsOrderExpired(DateTimeOffset now, DateTimeOffset expiresAt)
        => expiresAt <= now;

    public static DateTimeOffset GetRenewalBaseDate(DateTimeOffset now, DateTimeOffset currentEndAt)
        => currentEndAt > now ? currentEndAt : now;

    public static DateTimeOffset GetGracePeriodEnd(DateTimeOffset endAt, int graceDays = 3)
        => endAt.AddDays(graceDays);
}
