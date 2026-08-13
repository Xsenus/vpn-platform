using System.Data.Common;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VpnPlatform.Application.Abstractions;
using VpnPlatform.Application.DTOs;
using VpnPlatform.Application.Services;
using VpnPlatform.Domain.Entities;
using VpnPlatform.Domain.Enums;
using VpnPlatform.Infrastructure.HostedServices;
using VpnPlatform.Infrastructure.Persistence;
using Xunit;

namespace VpnPlatform.UnitTests;

public class OwnVpsProvisioningReadBoundaryTests
{
    [Fact]
    public async Task OwnVpsAccess_Should_Select_Latest_Subscription_In_Sql()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var interceptor = new CommandCaptureInterceptor();
        await using var db = CreateDb(connection, interceptor);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 13, 10, 0, 0, TimeSpan.Zero);
        var user = new User { Email = "own-vps-boundary@example.test", PasswordHash = "hash", RolesCsv = "User", ReferralCode = "OWNVPSBOUNDARY" };
        var node = new VpnNode { Name = "Own VPS boundary", Host = "own-vps-boundary.example.test", SshUser = "root", SshPort = 22 };
        var tariff = new Tariff { Name = "Own VPS", Slug = "own-vps-mvp", DurationDays = 30, MaxDevices = 3, IsActive = false };
        var older = SubscriptionFor(user.Id, node.Id, tariff.Id, now.AddDays(-2));
        var latest = SubscriptionFor(user.Id, node.Id, tariff.Id, now.AddDays(-1));
        var run = new ProvisioningRun { NodeId = node.Id, RequestedByUserId = user.Id, Status = ProvisioningRunStatus.Deploying };
        db.AddRange(user, node, tariff, older, latest, run);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        interceptor.Commands.Clear();

        var method = typeof(ProvisioningWorker).GetMethod("EnsureOwnVpsAccessAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
        var result = await (Task<string>)method.Invoke(null, new object[]
        {
            db,
            new TestVpnProviderFactory(),
            node,
            run,
            new FixedClock(now),
            CancellationToken.None
        })!;

        Assert.Contains(latest.Id.ToString(), result, StringComparison.Ordinal);
        Assert.Contains(interceptor.Commands, command =>
            IsSelectFor(command, "Subscriptions")
            && command.Contains("LIMIT 1", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task OwnVpsSupport_Should_Select_Latest_Open_Conversation_In_Sql()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var interceptor = new CommandCaptureInterceptor();
        await using var db = CreateDb(connection, interceptor);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 13, 10, 0, 0, TimeSpan.Zero);
        var user = new User { Email = "own-vps-support-boundary@example.test", PasswordHash = "hash", RolesCsv = "User", ReferralCode = "OWNVPSSUPPORT" };
        var node = new VpnNode { Name = "Own VPS support boundary", Host = "own-vps-support.example.test", SshUser = "root", SshPort = 22, TagsCsv = "telegram-user-id:5050" };
        var run = new ProvisioningRun { NodeId = node.Id, RequestedByUserId = user.Id, Status = ProvisioningRunStatus.PrecheckFailed };
        var older = ConversationFor(user.Id, now.AddDays(-2), "pending", revision: 2);
        var latest = ConversationFor(user.Id, now.AddDays(-1), "pending", revision: 4);
        db.AddRange(user, node, run, older, latest);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        interceptor.Commands.Clear();

        var method = typeof(ProvisioningWorker).GetMethod("EnsureSupportConversationAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
        await (Task)method.Invoke(null, new object[]
        {
            db,
            node,
            run,
            "Own VPS precheck failed",
            "precheck failed",
            new FixedClock(now),
            CancellationToken.None
        })!;
        await db.SaveChangesAsync();

        Assert.Equal(2, await db.SupportConversations.CountAsync());
        Assert.Equal(2, (await db.SupportConversations.AsNoTracking().SingleAsync(x => x.Id == older.Id)).Revision);
        Assert.Equal(5, (await db.SupportConversations.AsNoTracking().SingleAsync(x => x.Id == latest.Id)).Revision);
        Assert.Equal(latest.Id, (await db.SupportMessages.AsNoTracking().SingleAsync()).SupportConversationId);
        Assert.Contains(interceptor.Commands, command =>
            IsSelectFor(command, "SupportConversations")
            && command.Contains("LIMIT 1", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MarkSupportNeeded_Should_Select_Latest_Conversation_In_Sql_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var interceptor = new CommandCaptureInterceptor();
        await using var db = CreateDb(connection, interceptor);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 13, 19, 15, 0, TimeSpan.Zero);
        var user = new User { Email = "provisioning-service-support@example.test", PasswordHash = "hash", RolesCsv = "User", ReferralCode = "PROVSERVSUPPORT" };
        var node = new VpnNode { Name = "Provisioning service support", Host = "provisioning-support.example.test", SshUser = "root", SshPort = 22, TagsCsv = "telegram-user-id:5050" };
        var run = new ProvisioningRun
        {
            NodeId = node.Id,
            RequestedByUserId = user.Id,
            Status = ProvisioningRunStatus.PrecheckFailed,
            ExecutionLog = "Precheck failed."
        };
        var older = ServiceConversationFor(user.Id, now.AddDays(-2), revision: 2);
        var latest = ServiceConversationFor(user.Id, now.AddDays(-1), revision: 4);
        db.AddRange(user, node, run, older, latest);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        interceptor.Commands.Clear();

        var result = await new ProvisioningService(db, new FixedClock(now))
            .MarkSupportNeededAsync(run.Id, user.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(latest.Id.ToString(), result.Value);
        Assert.Equal(2, await db.SupportConversations.CountAsync());
        Assert.Equal(2, (await db.SupportConversations.AsNoTracking().SingleAsync(x => x.Id == older.Id)).Revision);
        Assert.Equal(5, (await db.SupportConversations.AsNoTracking().SingleAsync(x => x.Id == latest.Id)).Revision);
        Assert.Equal(latest.Id, (await db.SupportMessages.AsNoTracking().SingleAsync()).SupportConversationId);
        Assert.Contains(interceptor.Commands, command =>
            IsSelectFor(command, "SupportConversations")
            && command.Contains("julianday", StringComparison.OrdinalIgnoreCase)
            && command.Contains("LIMIT 1", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MarkSupportNeeded_Should_Reuse_Conversation_With_Null_Identities_On_Sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var interceptor = new CommandCaptureInterceptor();
        await using var db = CreateDb(connection, interceptor);
        await db.Database.EnsureCreatedAsync();
        var now = new DateTimeOffset(2026, 8, 13, 19, 20, 0, TimeSpan.Zero);
        var node = new VpnNode { Name = "System provisioning support", Host = "system-support.example.test", SshUser = "root", SshPort = 22 };
        var run = new ProvisioningRun
        {
            NodeId = node.Id,
            Status = ProvisioningRunStatus.PrecheckFailed,
            ExecutionLog = "System precheck failed."
        };
        var conversation = ServiceConversationFor(userId: null, createdAt: now.AddDays(-1), revision: 3, telegramUserId: null);
        db.AddRange(node, run, conversation);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        interceptor.Commands.Clear();

        var result = await new ProvisioningService(db, new FixedClock(now))
            .MarkSupportNeededAsync(run.Id, requestedByUserId: null);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(conversation.Id.ToString(), result.Value);
        Assert.Single(await db.SupportConversations.ToListAsync());
        Assert.Equal(4, (await db.SupportConversations.AsNoTracking().SingleAsync()).Revision);
        Assert.Equal(conversation.Id, (await db.SupportMessages.AsNoTracking().SingleAsync()).SupportConversationId);
        Assert.Contains(interceptor.Commands, command =>
            IsSelectFor(command, "SupportConversations")
            && command.Contains("IS NULL", StringComparison.OrdinalIgnoreCase)
            && command.Contains("LIMIT 1", StringComparison.OrdinalIgnoreCase));
    }

    private static Subscription SubscriptionFor(Guid userId, Guid nodeId, Guid tariffId, DateTimeOffset createdAt)
        => new()
        {
            UserId = userId,
            TariffId = tariffId,
            CurrentServerId = nodeId,
            Status = SubscriptionStatus.Active,
            StartAt = createdAt,
            EndAt = createdAt.AddDays(30),
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

    private static SupportConversation ConversationFor(Guid userId, DateTimeOffset createdAt, string status, int revision)
        => new()
        {
            UserId = userId,
            TelegramUserId = 5050,
            Channel = "telegram",
            Status = status,
            Subject = "Own VPS precheck failed",
            Revision = revision,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

    private static SupportConversation ServiceConversationFor(Guid? userId, DateTimeOffset createdAt, int revision, long? telegramUserId = 5050)
        => new()
        {
            UserId = userId,
            TelegramUserId = telegramUserId,
            Channel = "telegram",
            Status = "pending",
            Subject = "Own VPS provisioning needs support",
            Revision = revision,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

    private static bool IsSelectFor(string command, string table)
        => command.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            && command.Contains(table, StringComparison.OrdinalIgnoreCase);

    private static ApplicationDbContext CreateDb(SqliteConnection connection, DbCommandInterceptor interceptor)
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options);

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Commands.Add(command.CommandText);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class TestVpnProviderFactory : IVpnProviderFactory
    {
        private readonly TestVpnProvider _provider = new();
        public IVpnProvider Get(string providerName) => _provider;
    }

    private sealed class TestVpnProvider : IVpnProvider
    {
        public string Name => "x3ui";

        public Task<VpnProvisionResult> CreateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new VpnProvisionResult(
                $"client-{request.SubscriptionId:N}",
                $"vless://{request.SubscriptionId:N}@own-vps.example.test",
                "qr://own-vps",
                "config://own-vps"));

        public Task<VpnProvisionResult> UpdateAccessAsync(VpnProvisionRequest request, CancellationToken cancellationToken)
            => CreateAccessAsync(request, cancellationToken);

        public Task DisableAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAccessAsync(string providerAccessId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<VpnUsageSnapshot> GetUsageAsync(string providerAccessId, CancellationToken cancellationToken)
            => Task.FromResult(new VpnUsageSnapshot(providerAccessId, 0, 0, DateTimeOffset.UtcNow));
        public Task<HealthStatus> GetNodeHealthAsync(VpnNode node, CancellationToken cancellationToken)
            => Task.FromResult(HealthStatus.Healthy);
    }
}
