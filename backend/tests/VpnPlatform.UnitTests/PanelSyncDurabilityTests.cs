using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class PanelSyncDurabilityTests
{
    [Fact]
    public async Task Concurrent_Sqlite_Sync_Should_Allow_Only_One_Remote_Call()
    {
        var databaseName = $"panel-sync-{Guid.NewGuid():N}";
        var connectionString = $"Data Source={databaseName};Mode=Memory;Cache=Shared";
        await using var anchor = new SqliteConnection(connectionString);
        await anchor.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connectionString).Options;
        Guid panelId;
        await using (var setup = new ApplicationDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            var panel = CreatePanel();
            panelId = panel.Id;
            setup.VpnPanels.Add(panel);
            await setup.SaveChangesAsync();
        }

        var client = new BlockingX3UiClient();
        await using var firstDb = new ApplicationDbContext(options);
        await using var secondDb = new ApplicationDbContext(options);
        var firstService = CreateService(firstDb, client);
        var secondService = CreateService(secondDb, client);

        var first = firstService.SyncPanelAsync(panelId, CancellationToken.None);
        await client.FirstCallStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = secondService.SyncPanelAsync(panelId, CancellationToken.None);
        var secondCompletion = await Task.WhenAny(second, Task.Delay(TimeSpan.FromSeconds(2)));
        client.Release.TrySetResult();
        var firstResult = await first;
        var secondResult = await second;

        Assert.Same(second, secondCompletion);
        Assert.True(firstResult.IsSuccess);
        Assert.False(secondResult.IsSuccess);
        Assert.Contains("already running", secondResult.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, client.GetInboundsCalls);
        await using var assertDb = new ApplicationDbContext(options);
        Assert.Single(await assertDb.PanelSyncRuns.Where(x => x.VpnPanelId == panelId && x.Status == PanelSyncRunStatus.Succeeded).ToListAsync());
        Assert.DoesNotContain(await assertDb.PanelSyncRuns.ToListAsync(), x => x.Status == PanelSyncRunStatus.Running);

        await using var staleDb = new ApplicationDbContext(options);
        var staleResult = await CreateService(staleDb, client).SyncPanelIfCurrentAsync(
            panelId,
            expectedLastSyncAt: null,
            cancellationToken: CancellationToken.None);
        Assert.False(staleResult.IsSuccess);
        Assert.Contains("stale", staleResult.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, client.GetInboundsCalls);
    }

    [Fact]
    public async Task Sync_Should_Recover_Expired_Running_Lease_Before_New_Attempt()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var panel = CreatePanel();
        var staleRun = new PanelSyncRun
        {
            VpnPanelId = panel.Id,
            Status = PanelSyncRunStatus.Running,
            StartedAt = FixedClock.Now.AddMinutes(-10)
        };
        db.AddRange(panel, staleRun);
        await db.SaveChangesAsync();
        var service = CreateService(db, new BlockingX3UiClient { Block = false });

        var result = await service.SyncPanelAsync(panel.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        db.ChangeTracker.Clear();
        var recovered = await db.PanelSyncRuns.SingleAsync(x => x.Id == staleRun.Id);
        Assert.Equal(PanelSyncRunStatus.Failed, recovered.Status);
        Assert.NotNull(recovered.FinishedAt);
        Assert.Contains("lease expired", recovered.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Single(await db.PanelSyncRuns.Where(x => x.VpnPanelId == panel.Id && x.Status == PanelSyncRunStatus.Succeeded).ToListAsync());
    }

    [Fact]
    public async Task Health_Check_Should_Persist_Redacted_Failure_When_Secret_Cannot_Be_Decrypted()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new ApplicationDbContext(options);
        var panel = CreatePanel();
        db.VpnPanels.Add(panel);
        await db.SaveChangesAsync();
        var service = new X3UiPanelService(
            db,
            new BlockingX3UiClient { Block = false },
            new ThrowingSecretProtector(),
            new FixedClock(),
            ProductionConfiguration());

        var result = await service.CheckHealthAsync(panel.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.DoesNotContain("top-secret", result.Error, StringComparison.Ordinal);
        var check = await db.PanelHealthChecks.SingleAsync(x => x.VpnPanelId == panel.Id);
        Assert.Equal(HealthStatus.Unhealthy, check.Status);
        Assert.DoesNotContain("top-secret", check.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("REDACTED", check.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        var savedPanel = await db.VpnPanels.SingleAsync(x => x.Id == panel.Id);
        Assert.Equal(check.CheckedAt, savedPanel.LastHealthCheckAt);
        Assert.Equal(check.ErrorMessage, savedPanel.LastError);
    }

    private static X3UiPanelService CreateService(ApplicationDbContext db, IX3UiClient client)
        => new(db, client, new PassThroughSecretProtector(), new FixedClock(), ProductionConfiguration());

    private static IConfiguration ProductionConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Vpn:X3Ui:Mode"] = "Production" })
            .Build();

    private static VpnPanel CreatePanel()
        => new()
        {
            Id = Guid.NewGuid(),
            Name = $"panel-{Guid.NewGuid():N}",
            BaseUrl = "https://panel.example.test",
            Login = "admin",
            EncryptedPassword = "protected-secret",
            Region = "test",
            Status = VpnPanelStatus.Active,
            HealthStatus = HealthStatus.Unknown,
            Capacity = 100
        };

    private sealed class FixedClock : IClock
    {
        public static DateTimeOffset Now { get; } = new(2026, 8, 4, 15, 0, 0, TimeSpan.Zero);
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class PassThroughSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue) => protectedValue;
        public string Mask(string? value, int visibleTail = 4) => "***";
    }

    private sealed class ThrowingSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue)
            => throw new InvalidOperationException("password=top-secret token=provider-token");
        public string Mask(string? value, int visibleTail = 4) => "***";
    }

    private sealed class BlockingX3UiClient : IX3UiClient
    {
        private int _getInboundsCalls;
        public bool Block { get; init; } = true;
        public int GetInboundsCalls => Volatile.Read(ref _getInboundsCalls);
        public TaskCompletionSource FirstCallStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<X3UiSession> LoginAsync(VpnPanel panel, string password, CancellationToken cancellationToken)
            => Task.FromResult(new X3UiSession("session=test", FixedClock.Now));

        public Task<X3UiHealthResult> CheckHealthAsync(VpnPanel panel, string password, CancellationToken cancellationToken)
            => Task.FromResult(new X3UiHealthResult(true, "test", 1));

        public Task<X3UiPanelVersionResult> GetPanelVersionAsync(VpnPanel panel, string password, CancellationToken cancellationToken)
            => Task.FromResult(new X3UiPanelVersionResult("test", "{}"));

        public async Task<IReadOnlyCollection<X3UiInboundDto>> GetInboundsAsync(VpnPanel panel, string password, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _getInboundsCalls);
            FirstCallStarted.TrySetResult();
            if (Block)
            {
                await Release.Task.WaitAsync(cancellationToken);
            }
            return Array.Empty<X3UiInboundDto>();
        }

        public Task<X3UiInboundDto?> GetInboundAsync(VpnPanel panel, string password, string inboundId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<X3UiInboundDto> CreateInboundAsync(VpnPanel panel, string password, X3UiCreateInboundRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteInboundAsync(VpnPanel panel, string password, string inboundId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<X3UiInboundDto> UpdateInboundAsync(VpnPanel panel, string password, X3UiUpdateInboundRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<X3UiClientDto> AddClientAsync(VpnPanel panel, string password, X3UiAddClientRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<X3UiClientDto> UpdateClientAsync(VpnPanel panel, string password, X3UiUpdateClientRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteClientAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task EnableClientAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DisableClientAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task ResetClientTrafficAsync(VpnPanel panel, string password, string inboundId, string clientId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<X3UiTrafficSnapshot> GetClientTrafficAsync(VpnPanel panel, string password, string clientId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
