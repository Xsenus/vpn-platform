using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class AuditableEntityPersistenceTests
{
    [Fact]
    public void Domain_Entities_Should_Not_Capture_Wall_Clock_On_Construction()
    {
        Assert.Equal(default, new InboxMessage().ReceivedAt);
        Assert.Equal(default, new ProvisioningRun().StartedAt);
        Assert.Equal(default, new ProvisioningStepRun().StartedAt);
        Assert.Equal(default, new MigrationJob().RequestedAt);
        Assert.Equal(default, new NodeHealthCheck().CheckedAt);
        Assert.Equal(default, new ChannelProfile().LinkedAt);
        Assert.Equal(default, new PaymentWebhookEvent().ReceivedAt);
        Assert.Equal(default, new AccessCredential().IssuedAt);
        Assert.Equal(default, new AppRelease().ReleasedAt);
        Assert.Equal(default, new AppReleaseSeen().SeenAt);
    }

    [Fact]
    public async Task Async_Save_Should_Stamp_New_Entities_With_Injected_Clock()
    {
        var now = new DateTimeOffset(2035, 1, 2, 3, 4, 5, TimeSpan.Zero);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDbContext(connection, new FixedClock(now));
        await db.Database.EnsureCreatedAsync();
        var user = NewUser();
        db.Users.Add(user);

        await db.SaveChangesAsync();

        Assert.Equal(now, user.CreatedAt);
        Assert.Equal(now, user.UpdatedAt);
    }

    [Fact]
    public void Sync_Save_Should_Preserve_Explicit_Creation_Time_And_Fill_Missing_Update_Time()
    {
        var now = new DateTimeOffset(2035, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var historical = now.AddYears(-2);
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var db = CreateDbContext(connection, new FixedClock(now));
        db.Database.EnsureCreated();
        var user = NewUser();
        user.CreatedAt = historical;
        db.Users.Add(user);

        db.SaveChanges();

        Assert.Equal(historical, user.CreatedAt);
        Assert.Equal(historical, user.UpdatedAt);
    }

    private static ApplicationDbContext CreateDbContext(SqliteConnection connection, IClock clock)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options, clock);

    private static User NewUser()
        => new()
        {
            Email = $"audit-{Guid.NewGuid():N}@example.test",
            DisplayName = "Audit User",
            PasswordHash = "hash",
            ReferralCode = $"audit-{Guid.NewGuid():N}"
        };

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
