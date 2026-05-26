using VpnPlatform.Application.Common;
using Xunit;

namespace VpnPlatform.UnitTests;

public class OrderLifecycleTests
{
    [Fact]
    public void Order_Should_Expire_When_ExpiresAt_Is_In_The_Past()
    {
        var now = new DateTimeOffset(2026, 3, 25, 12, 0, 0, TimeSpan.Zero);
        var expiresAt = now.AddMinutes(-1);

        Assert.True(BusinessRules.IsOrderExpired(now, expiresAt));
    }

    [Fact]
    public void Renewal_Should_Extend_From_EndAt_When_Subscription_Is_Still_Active()
    {
        var now = new DateTimeOffset(2026, 3, 25, 12, 0, 0, TimeSpan.Zero);
        var currentEndAt = now.AddDays(10);

        var baseDate = BusinessRules.GetRenewalBaseDate(now, currentEndAt);

        Assert.Equal(currentEndAt, baseDate);
    }

    [Fact]
    public void Renewal_Should_Extend_From_Now_When_Subscription_Already_Expired()
    {
        var now = new DateTimeOffset(2026, 3, 25, 12, 0, 0, TimeSpan.Zero);
        var currentEndAt = now.AddDays(-2);

        var baseDate = BusinessRules.GetRenewalBaseDate(now, currentEndAt);

        Assert.Equal(now, baseDate);
    }

    [Fact]
    public void Grace_Period_Should_Default_To_Three_Days()
    {
        var endAt = new DateTimeOffset(2026, 3, 25, 12, 0, 0, TimeSpan.Zero);

        var graceEnd = BusinessRules.GetGracePeriodEnd(endAt);

        Assert.Equal(endAt.AddDays(3), graceEnd);
    }
}
