using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Api.Controllers.Admin;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.HostedServices;
using VpnPlatform.Infrastructure.Persistence;
using VpnPlatform.Infrastructure.Provisioning;
using Xunit;

namespace VpnPlatform.UnitTests;

public class ProvisioningRunCoordinatorTests
{
    [Fact]
    public async Task TryClaimAsync_Should_Allow_Only_One_Concurrent_Worker()
    {
        var databasePath = TemporaryDatabasePath();
        try
        {
            var runId = Guid.NewGuid();
            await SeedQueuedRunAsync(databasePath, runId);
            var clock = new FixedClock(new DateTimeOffset(2026, 8, 4, 14, 0, 0, TimeSpan.Zero));
            await using var firstDb = CreateDb(databasePath);
            await using var secondDb = CreateDb(databasePath);
            var first = Coordinator(firstDb, clock);
            var second = Coordinator(secondDb, clock);

            var claims = await Task.WhenAll(first.TryClaimAsync(runId), second.TryClaimAsync(runId));

            Assert.Single(claims, claimed => claimed);
            await using var assertDb = CreateDb(databasePath);
            var run = await assertDb.ProvisioningRuns.AsNoTracking().SingleAsync();
            Assert.Equal(ProvisioningRunStatus.Prechecking, run.Status);
            Assert.Equal(1, run.AttemptCount);
            Assert.Equal(clock.UtcNow, run.ProcessingStartedAt);
            Assert.True(run.LeaseExpiresAt > clock.UtcNow);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Theory]
    [InlineData(ProvisioningRunStatus.Prechecking, true, ProvisioningRunStatus.PrecheckFailed)]
    [InlineData(ProvisioningRunStatus.Deploying, false, ProvisioningRunStatus.Failed)]
    public async Task RecoverExpiredClaimsAsync_Should_Quarantine_Run_For_Operator_Review(
        ProvisioningRunStatus currentStatus,
        bool dryRun,
        ProvisioningRunStatus expectedStatus)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new FixedClock(new DateTimeOffset(2026, 8, 4, 14, 0, 0, TimeSpan.Zero));
        var node = Node();
        var run = new ProvisioningRun
        {
            NodeId = node.Id,
            Status = currentStatus,
            DryRun = dryRun,
            AttemptCount = 1,
            ProcessingStartedAt = clock.UtcNow.AddHours(-2),
            LeaseExpiresAt = clock.UtcNow.AddMinutes(-1),
            UpdatedAt = clock.UtcNow.AddHours(-2)
        };
        db.AddRange(node, run);
        await db.SaveChangesAsync();

        var recovered = await Coordinator(db, clock).RecoverExpiredClaimsAsync();

        Assert.Equal(1, recovered);
        await db.Entry(run).ReloadAsync();
        await db.Entry(node).ReloadAsync();
        Assert.Equal(expectedStatus, run.Status);
        Assert.Null(run.ProcessingStartedAt);
        Assert.Null(run.LeaseExpiresAt);
        Assert.Contains("Automatic replay is blocked", run.LastError, StringComparison.Ordinal);
        Assert.Equal(NodeStatus.Error, node.Status);
        Assert.False(node.IsAvailableForNewUsers);
        Assert.Contains(await db.ProvisioningStepRuns.ToListAsync(), x => x.ProvisioningRunId == run.Id && x.StepName == "Worker lease recovery");
        Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.EntityId == run.Id.ToString() && x.Action == "provisioning.worker_claim_failed");
    }

    [Fact]
    public async Task Worker_Support_Message_Should_Reopen_Pending_Conversation_And_Advance_Revision()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync();
        var clock = new FixedClock(new DateTimeOffset(2026, 8, 5, 5, 30, 0, TimeSpan.Zero));
        var node = Node();
        node.TagsCsv = "telegram-user-id:5050";
        var run = new ProvisioningRun { NodeId = node.Id, Status = ProvisioningRunStatus.PrecheckFailed, DryRun = true };
        var conversation = new SupportConversation
        {
            TelegramUserId = 5050,
            Channel = "telegram",
            Status = "pending",
            Subject = "Own VPS precheck failed",
            Revision = 4
        };
        db.AddRange(node, run, conversation);
        await db.SaveChangesAsync();

        var ensureSupport = typeof(ProvisioningWorker).GetMethod("EnsureSupportConversationAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
        await (Task)ensureSupport.Invoke(null, new object[]
        {
            db,
            node,
            run,
            "Own VPS precheck failed",
            "precheck failed",
            clock,
            CancellationToken.None
        })!;
        await db.SaveChangesAsync();

        Assert.Equal(1, await db.SupportConversations.CountAsync());
        Assert.Equal("open", conversation.Status);
        Assert.Null(conversation.ClosedAt);
        Assert.Equal(5, conversation.Revision);
        Assert.Single(await db.SupportMessages.Where(x => x.SupportConversationId == conversation.Id).ToListAsync());
    }

    [Fact]
    public async Task ProvisioningWorker_Should_Execute_A_Claimed_Run_Only_Once()
    {
        var databasePath = TemporaryDatabasePath();
        try
        {
            var runId = Guid.NewGuid();
            await SeedQueuedRunAsync(databasePath, runId);
            var executor = new BlockingExecutor();
            using var services = BuildWorkerServices(databasePath, executor);
            var firstWorker = new ProvisioningWorker(services, NullLogger<ProvisioningWorker>.Instance);
            var secondWorker = new ProvisioningWorker(services, NullLogger<ProvisioningWorker>.Instance);
            var processNext = typeof(ProvisioningWorker).GetMethod("ProcessNextRunAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

            var firstTask = (Task<bool>)processNext.Invoke(firstWorker, new object[] { CancellationToken.None })!;
            await executor.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var secondResult = await (Task<bool>)processNext.Invoke(secondWorker, new object[] { CancellationToken.None })!;
            executor.Release.TrySetResult();
            var firstResult = await firstTask;

            Assert.True(firstResult);
            Assert.False(secondResult);
            Assert.Equal(1, executor.ExecutionCount);
            await using var assertDb = CreateDb(databasePath);
            var run = await assertDb.ProvisioningRuns.AsNoTracking().SingleAsync();
            Assert.Equal(ProvisioningRunStatus.ReadyToDeploy, run.Status);
            Assert.Equal(1, run.AttemptCount);
            Assert.Null(run.LeaseExpiresAt);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task QueueAsync_Should_Create_Only_One_Active_Run_For_Concurrent_Requests()
    {
        var databasePath = TemporaryDatabasePath();
        try
        {
            var node = Node();
            await using (var setupDb = CreateDb(databasePath))
            {
                await setupDb.Database.EnsureCreatedAsync();
                setupDb.VpnNodes.Add(node);
                await setupDb.SaveChangesAsync();
            }

            await using var firstDb = CreateDb(databasePath);
            await using var secondDb = CreateDb(databasePath);
            var clock = new FixedClock(new DateTimeOffset(2026, 8, 4, 14, 0, 0, TimeSpan.Zero));
            var firstService = new ProvisioningService(firstDb, clock, new TestSecretProtector());
            var secondService = new ProvisioningService(secondDb, clock, new TestSecretProtector());

            var results = await Task.WhenAll(
                firstService.QueueAsync(node.Id, dryRun: true, requestedByUserId: Guid.NewGuid()),
                secondService.QueueAsync(node.Id, dryRun: true, requestedByUserId: Guid.NewGuid()));

            Assert.Single(results, result => result.IsSuccess);
            Assert.Single(results, result => !result.IsSuccess && result.Error!.Contains("already queued", StringComparison.OrdinalIgnoreCase));
            await using var assertDb = CreateDb(databasePath);
            Assert.Equal(1, await assertDb.ProvisioningRuns.CountAsync());
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task CancelAsync_Should_Not_Overwrite_A_Concurrent_Worker_Claim()
    {
        var databasePath = TemporaryDatabasePath();
        try
        {
            var runId = Guid.NewGuid();
            await SeedQueuedRunAsync(databasePath, runId);
            var clock = new FixedClock(new DateTimeOffset(2026, 8, 5, 13, 45, 0, TimeSpan.Zero));
            var claimBeforeCancel = new ClaimBeforeCancelTransactionInterceptor(databasePath, runId, clock);
            await using var cancelDb = CreateDb(databasePath, claimBeforeCancel);
            var cancelService = new ProvisioningService(cancelDb, clock, new TestSecretProtector());
            var controller = new AdminOperationsController(
                cancelDb,
                cancelService,
                paymentOrchestrator: null!,
                paymentProviderAccounts: null!,
                vpnAccessLifecycleService: null,
                secretProtector: new TestSecretProtector());
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            var cancel = await controller.CancelProvisioningRun(runId, CancellationToken.None);

            Assert.True(claimBeforeCancel.Claimed);
            var conflict = Assert.IsType<ConflictObjectResult>(cancel);
            Assert.Contains("state changed", JsonSerializer.Serialize(conflict.Value), StringComparison.OrdinalIgnoreCase);
            await using var assertDb = CreateDb(databasePath);
            var run = await assertDb.ProvisioningRuns.AsNoTracking().SingleAsync(x => x.Id == runId);
            var node = await assertDb.VpnNodes.AsNoTracking().SingleAsync(x => x.Id == run.NodeId);
            Assert.Equal(ProvisioningRunStatus.Prechecking, run.Status);
            Assert.NotEqual(ProvisioningRunStatus.Cancelled, node.ProvisioningStatus);
            Assert.DoesNotContain(await assertDb.AuditLogs.ToListAsync(), x => x.Action == "provisioning.cancel" && x.EntityId == runId.ToString());
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    private static ServiceProvider BuildWorkerServices(string databasePath, IProvisioningExecutor executor)
        => new ServiceCollection()
            .AddScoped(_ => CreateDb(databasePath))
            .AddScoped<ProvisioningRunCoordinator>()
            .AddSingleton<IClock>(new FixedClock(new DateTimeOffset(2026, 8, 4, 14, 0, 0, TimeSpan.Zero)))
            .AddSingleton(Options.Create(new ProvisioningOptions { ExecutionTimeoutSeconds = 60 }))
            .AddSingleton(executor)
            .AddSingleton<ISecretProtector, TestSecretProtector>()
            .AddSingleton<IVpnProviderFactory, UnusedVpnProviderFactory>()
            .BuildServiceProvider();

    private static ProvisioningRunCoordinator Coordinator(ApplicationDbContext db, IClock clock)
        => new(db, clock, Options.Create(new ProvisioningOptions { ExecutionTimeoutSeconds = 60 }));

    private static async Task SeedQueuedRunAsync(string databasePath, Guid runId)
    {
        await using var db = CreateDb(databasePath);
        await db.Database.EnsureCreatedAsync();
        var node = Node();
        db.VpnNodes.Add(node);
        db.ProvisioningRuns.Add(new ProvisioningRun
        {
            Id = runId,
            NodeId = node.Id,
            Status = ProvisioningRunStatus.PrecheckQueued,
            DryRun = true,
            ExecutionLog = "Precheck queued."
        });
        await db.SaveChangesAsync();
    }

    private static VpnNode Node()
        => new()
        {
            Name = "coordinator-test",
            Host = "provisioning.example.test",
            SshUser = "root",
            SshPort = 22,
            TagsCsv = "validation-mode:true",
            Status = NodeStatus.New,
            IsAvailableForNewUsers = false
        };

    private static ApplicationDbContext CreateDb(string databasePath)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite($"Data Source={databasePath};Default Timeout=10;Pooling=False").Options);

    private static ApplicationDbContext CreateDb(string databasePath, IInterceptor interceptor)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={databasePath};Default Timeout=10;Pooling=False")
            .AddInterceptors(interceptor)
            .Options);

    private static ApplicationDbContext CreateDb(SqliteConnection connection)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);

    private static string TemporaryDatabasePath()
        => Path.Combine(Path.GetTempPath(), $"vpn-platform-provisioning-{Guid.NewGuid():N}.db");

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class BlockingExecutor : IProvisioningExecutor
    {
        private int _executionCount;
        public int ExecutionCount => _executionCount;
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<ProvisioningExecutionResult> ExecuteAsync(VpnNode node, ProvisioningRun run, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _executionCount);
            Started.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return new ProvisioningExecutionResult(true, "Precheck passed.", Array.Empty<ProvisioningStepResult>());
        }
    }

    private sealed class ClaimBeforeCancelTransactionInterceptor(
        string databasePath,
        Guid runId,
        IClock clock) : DbTransactionInterceptor
    {
        private int _intercepted;
        public bool Claimed { get; private set; }

        public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
            DbConnection connection,
            TransactionStartingEventData eventData,
            InterceptionResult<DbTransaction> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _intercepted, 1) == 0)
            {
                await using var claimDb = CreateDb(databasePath);
                Claimed = await Coordinator(claimDb, clock).TryClaimAsync(runId, cancellationToken);
            }

            return result;
        }
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        public string Protect(string plaintext) => "v1:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext));
        public string Unprotect(string protectedValue) => Encoding.UTF8.GetString(Convert.FromBase64String(protectedValue[3..]));
        public string Mask(string? value, int visibleTail = 4) => "***";
    }

    private sealed class UnusedVpnProviderFactory : IVpnProviderFactory
    {
        public IVpnProvider Get(string providerName) => throw new InvalidOperationException("Dry-run precheck must not resolve a VPN provider.");
    }
}
