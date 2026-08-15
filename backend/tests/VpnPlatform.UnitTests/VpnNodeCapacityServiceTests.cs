using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VpnPlatform.Application.Common;
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
                var node = await verify.VpnNodes.SingleAsync();
                Assert.Equal(1, node.UsedCapacity);
                Assert.Equal(1, node.Revision);
            }

            Assert.True(await new VpnNodeCapacityService(firstDb).ReleaseAsync(nodeId));
            Assert.True(await new VpnNodeCapacityService(secondDb).TryReserveAsync(nodeId));
            await using (var verify = new ApplicationDbContext(options))
            {
                var node = await verify.VpnNodes.SingleAsync();
                Assert.Equal(1, node.UsedCapacity);
                Assert.Equal(3, node.Revision);
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
        Assert.Equal(2, node.Revision);
    }

    [Fact]
    public async Task Reservation_Should_Wait_For_Server_State_Gate()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"vpn-node-capacity-{Guid.NewGuid():N}")
            .Options;
        await using var db = new ApplicationDbContext(options);
        var node = CreateNode(Guid.NewGuid(), capacity: 2);
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();
        var service = new VpnNodeCapacityService(db);
        await using var gate = await PaymentProcessingGate.AcquireVpnNodeStateAsync(node.Id, CancellationToken.None);

        var reservation = service.TryReserveAsync(node.Id);
        await Task.Delay(100);
        var completedBeforeRelease = reservation.IsCompleted;
        await gate.DisposeAsync();

        Assert.True(await reservation);
        Assert.False(completedBeforeRelease);
        Assert.Equal(1, node.UsedCapacity);
    }

    [Theory]
    [InlineData(NodeStatus.Archived, true)]
    [InlineData(NodeStatus.Disabled, true)]
    [InlineData(NodeStatus.Maintenance, true)]
    [InlineData(NodeStatus.Ready, false)]
    public async Task Reservation_Should_Reject_NonOperational_Node(NodeStatus status, bool available)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var node = CreateNode(Guid.NewGuid(), capacity: 2);
        node.Status = status;
        node.IsAvailableForNewUsers = available;
        node.Revision = 3;
        db.VpnNodes.Add(node);
        await db.SaveChangesAsync();

        var reserved = await new VpnNodeCapacityService(db).TryReserveAsync(node.Id);

        Assert.False(reserved);
        db.ChangeTracker.Clear();
        var persisted = await db.VpnNodes.SingleAsync(x => x.Id == node.Id);
        Assert.Equal(0, persisted.UsedCapacity);
        Assert.Equal(3, persisted.Revision);
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
