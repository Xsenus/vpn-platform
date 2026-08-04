using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class VpnNodeCapacityServiceTests
{
    [Fact]
    public async Task Concurrent_Reservations_Should_Not_Exceed_Last_Sqlite_Slot()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"vpn-node-capacity-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={databasePath};Default Timeout=30")
                .Options;
            var nodeId = Guid.NewGuid();
            await using (var seed = new ApplicationDbContext(options))
            {
                await seed.Database.EnsureCreatedAsync();
                seed.VpnNodes.Add(CreateNode(nodeId, capacity: 1));
                await seed.SaveChangesAsync();
            }

            await using var firstDb = new ApplicationDbContext(options);
            await using var secondDb = new ApplicationDbContext(options);
            var results = await Task.WhenAll(
                new VpnNodeCapacityService(firstDb).TryReserveAsync(nodeId),
                new VpnNodeCapacityService(secondDb).TryReserveAsync(nodeId));

            Assert.Equal(1, results.Count(x => x));
            await using (var verify = new ApplicationDbContext(options))
            {
                Assert.Equal(1, (await verify.VpnNodes.SingleAsync()).UsedCapacity);
            }

            Assert.True(await new VpnNodeCapacityService(firstDb).ReleaseAsync(nodeId));
            Assert.True(await new VpnNodeCapacityService(secondDb).TryReserveAsync(nodeId));
            await using (var verify = new ApplicationDbContext(options))
            {
                Assert.Equal(1, (await verify.VpnNodes.SingleAsync()).UsedCapacity);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task InMemory_Reservation_And_Release_Should_Keep_Counter_In_Range()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"vpn-node-capacity-{Guid.NewGuid():N}")
            .Options;
        await using var db = new ApplicationDbContext(options);
        var node = CreateNode(Guid.NewGuid(), capacity: 1);
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();
        var service = new VpnNodeCapacityService(db);

        Assert.True(await service.TryReserveAsync(node.Id));
        Assert.False(await service.TryReserveAsync(node.Id));
        Assert.True(await service.ReleaseAsync(node.Id));
        Assert.False(await service.ReleaseAsync(node.Id));
        Assert.Equal(0, node.UsedCapacity);
    }

    private static VpnNode CreateNode(Guid id, int capacity)
        => new()
        {
            Id = id,
            Name = $"node-{id:N}",
            Host = "vpn.example.test",
            IpAddress = "127.0.0.1",
            Provider = "x3ui",
            Region = "eu",
            Country = "DE",
            Datacenter = "test",
            Status = NodeStatus.Ready,
            Capacity = capacity,
            HealthStatus = HealthStatus.Healthy,
            IsAvailableForNewUsers = true,
            SupportedProtocolsCsv = "vless"
        };
}
