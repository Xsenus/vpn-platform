using System.Reflection;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Infrastructure.HostedServices;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Services;
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
            var firstOrderId = Guid.NewGuid();
            var secondOrderId = Guid.NewGuid();
            setupDb.OutboxMessages.AddRange(
                new OutboxMessage { Type = "OrderTimelineEvent", CorrelationId = "2", PayloadJson = JsonSerializer.Serialize(new { orderId = secondOrderId, eventType = "second" }), CreatedAt = DateTimeOffset.UtcNow.AddMinutes(1) },
                new OutboxMessage { Type = "OrderTimelineEvent", CorrelationId = "1", PayloadJson = JsonSerializer.Serialize(new { orderId = firstOrderId, eventType = "first" }), CreatedAt = DateTimeOffset.UtcNow });
            await setupDb.SaveChangesAsync();
        }

        var services = new ServiceCollection()
            .AddScoped(_ => new ApplicationDbContext(options))
            .AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>())
            .AddSingleton<IClock, TestClock>()
            .AddScoped<ReferralRewardService>()
            .AddScoped<IOutboxMessageSink, LocalOutboxMessageSink>()
            .AddScoped<OutboxMessageDeliveryService>()
            .BuildServiceProvider();
        var worker = new OutboxDispatcherWorker(services, NullLogger<OutboxDispatcherWorker>.Instance);
        var processBatch = typeof(OutboxDispatcherWorker).GetMethod("ProcessOutboxBatchAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

        await (Task)processBatch.Invoke(worker, new object[] { CancellationToken.None })!;

        await using var assertDb = new ApplicationDbContext(options);
        Assert.Equal(2, await assertDb.OutboxMessages.CountAsync(x => x.ProcessedAt != null));
        Assert.Equal(2, await assertDb.OutboxMessages.SumAsync(x => x.Attempts));
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
