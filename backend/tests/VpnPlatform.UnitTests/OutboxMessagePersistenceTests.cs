using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class OutboxMessagePersistenceTests
{
    [Fact]
    public async Task Concurrent_Equivalent_Events_Should_Persist_Once()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"vpn-platform-outbox-enqueue-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath};Cache=Shared;Default Timeout=10";
        try
        {
            await using (var setup = CreateDbContext(connectionString))
            {
                await setup.Database.EnsureCreatedAsync();
            }

            await using var first = CreateDbContext(connectionString);
            await using var second = CreateDbContext(connectionString);
            first.OutboxMessages.Add(Message());
            second.OutboxMessages.Add(Message());

            await Task.WhenAll(first.SaveChangesAsync(), second.SaveChangesAsync());

            await using var inspect = CreateDbContext(connectionString);
            Assert.Equal(1, await inspect.OutboxMessages.CountAsync());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public async Task Failed_Event_Should_Be_Revived_By_Equivalent_Enqueue()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var failed = Message();
        failed.Attempts = 10;
        failed.FailedAt = DateTimeOffset.UtcNow;
        failed.LastError = "old failure";
        db.OutboxMessages.Add(failed);
        await db.SaveChangesAsync();

        db.OutboxMessages.Add(Message("updated payload"));
        await db.SaveChangesAsync();

        var stored = await db.OutboxMessages.AsNoTracking().SingleAsync();
        Assert.Equal(failed.Id, stored.Id);
        Assert.Equal("updated payload", stored.PayloadJson);
        Assert.Equal(0, stored.Attempts);
        Assert.Null(stored.FailedAt);
        Assert.Null(stored.LastError);
    }

    [Fact]
    public async Task Distinct_Correlations_Should_Preserve_Separate_Events()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        db.OutboxMessages.AddRange(Message("pending", "payment:Pending"), Message("succeeded", "payment:Succeeded"));

        await db.SaveChangesAsync();

        Assert.Equal(2, await db.OutboxMessages.CountAsync());
    }

    [Fact]
    public async Task Duplicate_Events_In_One_Unit_Of_Work_Should_Collapse()
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        db.OutboxMessages.AddRange(Message(), Message());

        await db.SaveChangesAsync();

        Assert.Equal(1, await db.OutboxMessages.CountAsync());
    }

    [Theory]
    [InlineData("", "event")]
    [InlineData("OrderTimelineEvent", " ")]
    public async Task Missing_Event_Identity_Should_Fail_Closed(string type, string correlationId)
    {
        await using var connection = await OpenConnectionAsync();
        await using var db = CreateDbContext(connection);
        await db.Database.EnsureCreatedAsync();
        var message = Message();
        message.Type = type;
        message.CorrelationId = correlationId;
        db.OutboxMessages.Add(message);

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Equal(0, await db.OutboxMessages.CountAsync());
    }

    private static OutboxMessage Message(string payload = "{}", string correlationId = "same-event")
        => new()
        {
            Type = "OrderTimelineEvent",
            CorrelationId = correlationId,
            PayloadJson = payload
        };

    private static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static ApplicationDbContext CreateDbContext(string connectionString)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connectionString).Options);

    private static ApplicationDbContext CreateDbContext(SqliteConnection connection)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
}
