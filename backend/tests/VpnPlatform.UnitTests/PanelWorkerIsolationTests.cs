using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.HostedServices;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class PanelWorkerIsolationTests
{
    [Fact]
    public async Task Health_Worker_Should_Handle_Empty_Sqlite_Panel_List()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var services = CreateServices(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options,
            new WorkerX3UiClient(),
            new ConditionalSecretProtector());
        using (var scope = services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreatedAsync();
        }

        var worker = new PanelHealthWorker(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<PanelHealthWorker>.Instance);

        await worker.ProcessIterationAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Sync_Worker_Should_Handle_Empty_Sqlite_Panel_List()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var services = CreateServices(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options,
            new WorkerX3UiClient(),
            new ConditionalSecretProtector());
        using (var scope = services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreatedAsync();
        }

        var worker = new PanelSyncWorker(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<PanelSyncWorker>.Instance);

        await worker.ProcessIterationAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Health_Worker_Should_Continue_After_One_Panel_Fails_Before_Client_Call()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var client = new WorkerX3UiClient();
        await using var services = CreateServices(databaseName, client, new ConditionalSecretProtector());
        Guid failedPanelId;
        Guid healthyPanelId;
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            failedPanelId = Guid.NewGuid();
            healthyPanelId = Guid.NewGuid();
            db.VpnPanels.AddRange(
                CreatePanel(failedPanelId, "broken", "bad-secret", lastHealthCheckAt: null),
                CreatePanel(healthyPanelId, "healthy", "good-secret", TestClock.Now.AddHours(-1)));
            await db.SaveChangesAsync();
        }

        var worker = new PanelHealthWorker(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<PanelHealthWorker>.Instance);

        await worker.ProcessIterationAsync(CancellationToken.None);

        Assert.Equal(1, client.HealthCalls);
        using var assertScope = services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var failedPanel = await assertDb.VpnPanels.SingleAsync(x => x.Id == failedPanelId);
        Assert.Equal(TestClock.Now, failedPanel.LastHealthCheckAt);
        Assert.Equal(HealthStatus.Unhealthy, failedPanel.HealthStatus);
        Assert.Contains(await assertDb.PanelHealthChecks.ToListAsync(), x => x.VpnPanelId == failedPanelId && x.Status == HealthStatus.Unhealthy);
        Assert.Equal(TestClock.Now, (await assertDb.VpnPanels.SingleAsync(x => x.Id == healthyPanelId)).LastHealthCheckAt);
    }

    [Fact]
    public async Task Concurrent_Health_Workers_Should_Not_Check_Same_Observation_Twice()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var client = new WorkerX3UiClient { BlockHealth = true };
        await using var services = CreateServices(databaseName, client, new ConditionalSecretProtector());
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.VpnPanels.Add(CreatePanel(Guid.NewGuid(), "concurrent", "good-secret", lastHealthCheckAt: null));
            await db.SaveChangesAsync();
        }

        var firstWorker = new PanelHealthWorker(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<PanelHealthWorker>.Instance);
        var secondWorker = new PanelHealthWorker(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<PanelHealthWorker>.Instance);

        var first = firstWorker.ProcessIterationAsync(CancellationToken.None);
        await client.HealthStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = secondWorker.ProcessIterationAsync(CancellationToken.None);
        client.ReleaseHealth.TrySetResult();
        await Task.WhenAll(first, second);

        Assert.Equal(1, client.HealthCalls);
    }

    [Fact]
    public async Task Sync_Worker_Should_Update_Eligible_Panel()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var client = new WorkerX3UiClient();
        await using var services = CreateServices(databaseName, client, new ConditionalSecretProtector());
        var panelId = Guid.NewGuid();
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.VpnPanels.Add(CreatePanel(panelId, "sync", "good-secret", TestClock.Now.AddHours(-1)));
            await db.SaveChangesAsync();
        }

        var worker = new PanelSyncWorker(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<PanelSyncWorker>.Instance);

        await worker.ProcessIterationAsync(CancellationToken.None);

        Assert.Equal(1, client.GetInboundsCalls);
        using var assertScope = services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(TestClock.Now, (await assertDb.VpnPanels.SingleAsync(x => x.Id == panelId)).LastSyncAt);
        Assert.Contains(await assertDb.PanelSyncRuns.ToListAsync(), x => x.VpnPanelId == panelId && x.Status == PanelSyncRunStatus.Succeeded);
    }

    private static ServiceProvider CreateServices(string databaseName, IX3UiClient client, ISecretProtector protector)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return CreateServices(options, client, protector);
    }

    private static ServiceProvider CreateServices(DbContextOptions<ApplicationDbContext> options, IX3UiClient client, ISecretProtector protector)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Vpn:X3Ui:Mode"] = "Production" })
            .Build();
        return new ServiceCollection()
            .AddScoped(_ => new ApplicationDbContext(options))
            .AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>())
            .AddScoped<X3UiPanelService>()
            .AddSingleton<IClock, TestClock>()
            .AddSingleton(client)
            .AddSingleton(protector)
            .AddSingleton<IConfiguration>(configuration)
            .BuildServiceProvider();
    }

    private static VpnPanel CreatePanel(Guid id, string name, string encryptedPassword, DateTimeOffset? lastHealthCheckAt)
        => new()
        {
            Id = id,
            Name = name,
            BaseUrl = $"https://{name}.example.test",
            Login = "admin",
            EncryptedPassword = encryptedPassword,
            Region = "test",
            Status = VpnPanelStatus.Active,
            HealthStatus = HealthStatus.Unknown,
            Capacity = 100,
            LastHealthCheckAt = lastHealthCheckAt
        };

    private sealed class TestClock : IClock
    {
        public static DateTimeOffset Now { get; } = new(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class ConditionalSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => plaintext;
        public string Unprotect(string protectedValue)
            => protectedValue == "bad-secret" ? throw new InvalidOperationException("secret cannot be decrypted") : protectedValue;
        public string Mask(string? value, int visibleTail = 4) => "***";
    }

    private sealed class WorkerX3UiClient : IX3UiClient
    {
        public int HealthCalls { get; private set; }
        public int GetInboundsCalls { get; private set; }
        public bool BlockHealth { get; init; }
        public TaskCompletionSource HealthStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseHealth { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<X3UiSession> LoginAsync(VpnPanel panel, string password, CancellationToken cancellationToken)
            => Task.FromResult(new X3UiSession("session=test", TestClock.Now));

        public async Task<X3UiHealthResult> CheckHealthAsync(VpnPanel panel, string password, CancellationToken cancellationToken)
        {
            HealthCalls++;
            HealthStarted.TrySetResult();
            if (BlockHealth)
            {
                await ReleaseHealth.Task.WaitAsync(cancellationToken);
            }
            return new X3UiHealthResult(true, "test", 1);
        }

        public Task<X3UiPanelVersionResult> GetPanelVersionAsync(VpnPanel panel, string password, CancellationToken cancellationToken)
            => Task.FromResult(new X3UiPanelVersionResult("test", "{}"));

        public Task<IReadOnlyCollection<X3UiInboundDto>> GetInboundsAsync(VpnPanel panel, string password, CancellationToken cancellationToken)
        {
            GetInboundsCalls++;
            return Task.FromResult<IReadOnlyCollection<X3UiInboundDto>>(Array.Empty<X3UiInboundDto>());
        }

        public Task<X3UiInboundDto?> GetInboundAsync(VpnPanel panel, string password, string inboundId, CancellationToken cancellationToken)
            => Task.FromResult<X3UiInboundDto?>(null);
        public Task<X3UiInboundDto> CreateInboundAsync(VpnPanel panel, string password, X3UiCreateInboundRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteInboundAsync(VpnPanel panel, string password, string inboundId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<X3UiInboundDto> UpdateInboundAsync(VpnPanel panel, string password, X3UiUpdateInboundRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<X3UiClientDto> AddClientAsync(VpnPanel panel, string password, X3UiAddClientRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<X3UiClientDto> UpdateClientAsync(VpnPanel panel, string password, X3UiUpdateClientRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteClientAsync(VpnPanel panel, string password, string inboundId, string clientId, string email, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task EnableClientAsync(VpnPanel panel, string password, string inboundId, string clientId, string email, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DisableClientAsync(VpnPanel panel, string password, string inboundId, string clientId, string email, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task ResetClientTrafficAsync(VpnPanel panel, string password, string inboundId, string email, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<X3UiTrafficSnapshot> GetClientTrafficAsync(VpnPanel panel, string password, string email, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
