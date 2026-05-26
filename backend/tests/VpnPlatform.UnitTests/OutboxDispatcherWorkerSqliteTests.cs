using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.HostedServices;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class OutboxDispatcherWorkerSqliteTests
{
    [Fact]
    public async Task OutboxDispatcherWorker_Should_Process_First_Batch_With_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setupDb = new ApplicationDbContext(options))
        {
            await setupDb.Database.EnsureCreatedAsync();
            setupDb.OutboxMessages.AddRange(
                new OutboxMessage { Type = "second", CorrelationId = "2", PayloadJson = "{}", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(1) },
                new OutboxMessage { Type = "first", CorrelationId = "1", PayloadJson = "{}", CreatedAt = DateTimeOffset.UtcNow });
            await setupDb.SaveChangesAsync();
        }

        var services = new ServiceCollection()
            .AddScoped(_ => new ApplicationDbContext(options))
            .BuildServiceProvider();
        var worker = new OutboxDispatcherWorker(services, NullLogger<OutboxDispatcherWorker>.Instance);
        var processBatch = typeof(OutboxDispatcherWorker).GetMethod("ProcessOutboxBatchAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

        await (Task)processBatch.Invoke(worker, new object[] { CancellationToken.None })!;

        await using var assertDb = new ApplicationDbContext(options);
        Assert.Equal(2, await assertDb.OutboxMessages.CountAsync(x => x.ProcessedAt != null));
    }
}
